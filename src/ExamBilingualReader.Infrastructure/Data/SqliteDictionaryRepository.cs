using System.Data;
using System.IO;
using Dapper;
using ExamBilingualReader.Core.Models.Vocab;
using Microsoft.Data.Sqlite;

namespace ExamBilingualReader.Infrastructure.Data;

public interface IDictionaryRepository
{
    Task InitializeDatabaseAsync();
    Task<EcdictEntry?> FindWordAsync(string word);
    Task<Dictionary<string, EcdictEntry>> BatchLookupAsync(IEnumerable<string> words);
}

public class SqliteDictionaryRepository : IDictionaryRepository
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "ExamBilingualReader");

    public string DbPath { get; }

    public SqliteDictionaryRepository(string? dbPath = null)
    {
        if (!Directory.Exists(AppDataDir))
            Directory.CreateDirectory(AppDataDir);

        DbPath = dbPath ?? Path.Combine(AppDataDir, "ecdict.db");
    }

    private SqliteConnection CreateConnection() => new($"Data Source={DbPath}");

    public async Task InitializeDatabaseAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        // 创建 ECDICT 兼容核心表结构，并建立词条与原型索引加速文章分词查询
        var ddl = """
            CREATE TABLE IF NOT EXISTS ecdict (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                word TEXT NOT NULL UNIQUE,
                phonetic TEXT,
                definition TEXT,
                translation TEXT,
                pos TEXT,
                tag TEXT,
                collins INTEGER,
                oxford INTEGER,
                exchange TEXT,
                detail TEXT,
                audio TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_ecdict_word ON ecdict(word COLLATE NOCASE);
        """;

        await conn.ExecuteAsync(ddl);

        // 如果表中没有数据，插入一批四六级/雅思高频示例词条，确保开箱即用高亮体验
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM ecdict;");
        if (count == 0)
        {
            await SeedInitialSampleWordsAsync(conn);
        }
    }

    public async Task<EcdictEntry?> FindWordAsync(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return null;

        var clean = word.Trim().ToLowerInvariant();
        using var conn = CreateConnection();

        const string sql = """
            SELECT id, word, phonetic, definition, translation, pos, tag, collins, oxford, exchange, detail, audio
            FROM ecdict 
            WHERE word = @word COLLATE NOCASE
            LIMIT 1;
        """;

        return await conn.QueryFirstOrDefaultAsync<EcdictEntry>(sql, new { word = clean });
    }

    public async Task<Dictionary<string, EcdictEntry>> BatchLookupAsync(IEnumerable<string> words)
    {
        var distinctWords = words
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => w.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var result = new Dictionary<string, EcdictEntry>(StringComparer.OrdinalIgnoreCase);
        if (distinctWords.Count == 0)
            return result;

        using var conn = CreateConnection();
        await conn.OpenAsync();

        // 采用分批查询，防止超长 In 语句
        const int batchSize = 300;
        for (int i = 0; i < distinctWords.Count; i += batchSize)
        {
            var chunk = distinctWords.Skip(i).Take(batchSize).ToList();
            const string sql = """
                SELECT id, word, phonetic, definition, translation, pos, tag, collins, oxford, exchange, detail, audio
                FROM ecdict 
                WHERE word IN @chunk COLLATE NOCASE;
            """;

            var entries = await conn.QueryAsync<EcdictEntry>(sql, new { chunk });
            foreach (var entry in entries)
            {
                result[entry.Word] = entry;
            }
        }

        return result;
    }

    private static async Task SeedInitialSampleWordsAsync(IDbConnection conn)
    {
        var seedWords = new[]
        {
            new { word = "increase", phonetic = "ɪnˈkriːs", tag = "zk gk cet4", translation = "v. 增加，增大；n. 增加，增多", definition = "become or make greater in size, amount, or degree" },
            new { word = "decrease", phonetic = "dɪˈkriːs", tag = "zk gk cet4", translation = "v. 减少，减小；n. 减少，降低", definition = "make or become smaller or fewer in size, amount, intensity, or degree" },
            new { word = "cognitive", phonetic = "ˈkɒɡnətɪv", tag = "cet6 ielts toefl", translation = "adj. 认知的，认识能力的", definition = "relating to cognition" },
            new { word = "dense", phonetic = "dens", tag = "cet4 cet6 ielts", translation = "adj. 稠密的，密集的；浓厚的", definition = "closely compacted in substance" },
            new { word = "confirm", phonetic = "kənˈfɜːm", tag = "gk cet4 ielts", translation = "v. 证实，证明；确认，批准", definition = "state or prove the truth of" },
            new { word = "academic", phonetic = "ˌækəˈdemɪk", tag = "cet4 cet6 ielts", translation = "adj. 学术的；理论的；n. 学者", definition = "relating to education and scholarship" },
            new { word = "substantial", phonetic = "səbˈstænʃl", tag = "cet6 ielts toefl", translation = "adj. 大量的，价值巨大的；牢固的", definition = "of considerable importance, size, or worth" },
            new { word = "phenomenon", phonetic = "fəˈnɒmɪnən", tag = "cet4 cet6 ielts toefl", translation = "n. 现象；杰出的人或事", definition = "a fact or situation that is observed to exist or happen" },
            new { word = "ubiquitous", phonetic = "juːˈbɪkwɪtəs", tag = "ielts toefl gre", translation = "adj. 无所不在的，十分普遍的", definition = "present, appearing, or found everywhere" },
            new { word = "mitigate", phonetic = "ˈmɪtɪɡeɪt", tag = "ielts toefl gre", translation = "v. 缓解，减轻，平息", definition = "make less severe, serious, or painful" },
            new { word = "complex", phonetic = "ˈkɒmpleks", tag = "cet4 cet6", translation = "adj. 复杂的；综合的；n. 综合体", definition = "consisting of many different and connected parts" },
            new { word = "crucial", phonetic = "ˈkruːʃl", tag = "cet4 cet6 ielts", translation = "adj. 关键的，至关重要的", definition = "decisive or critical, especially in the success or failure of something" }
        };

        const string insertSql = """
            INSERT INTO ecdict (word, phonetic, tag, translation, definition)
            VALUES (@word, @phonetic, @tag, @translation, @definition);
        """;

        await conn.ExecuteAsync(insertSql, seedWords);
    }
}
