using System.Globalization;
using System.Text;

namespace TaskFlow.Domain.Common;

// Sigla do projeto que monta a chave da tarefa ("Portal do Cliente" -> PDC, então PDC-12).
// Mesma regra do site (utils/format.ts) e do app Android (domain/Format.kt): as chaves batem nos três.
public static class ProjectKey
{
    public static string For(string projectName)
    {
        // Sem acentos e só letras, números e espaços, em maiúsculas.
        var plain = new StringBuilder();
        foreach (var c in projectName.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                plain.Append(char.ToUpperInvariant(c));
        }

        var words = plain.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        return words.Length switch
        {
            0 => "TF",
            1 => words[0][..Math.Min(3, words[0].Length)],
            _ => string.Concat(words.Take(3).Select(w => w[0])),
        };
    }

    public static string TaskKey(string projectName, int number) => $"{For(projectName)}-{number}";
}
