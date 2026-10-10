namespace SharedKernel.Utilities.Helpers;

public static class IndianCurrencyWords
{
    private static readonly string[] Units =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    ];

    public static string ToRupeesOnly(decimal amount)
    {
        if (amount < 0) amount = 0;
        var rupees = (long)Math.Floor(amount);
        var paise = (int)Math.Round((amount - rupees) * 100m, MidpointRounding.AwayFromZero);
        if (paise == 100)
        {
            rupees += 1;
            paise = 0;
        }

        var words = rupees == 0 ? "Zero" : ConvertWholeNumber(rupees);
        var result = $"Rupees {words}";
        if (paise > 0)
            result += $" and {ConvertWholeNumber(paise)} Paise";
        return $"{result} Only".ToUpperInvariant();
    }

    private static string ConvertWholeNumber(long number)
    {
        if (number == 0) return "Zero";

        var parts = new List<string>();
        var crore = number / 10000000;
        number %= 10000000;
        var lakh = number / 100000;
        number %= 100000;
        var thousand = number / 1000;
        number %= 1000;
        var hundred = number / 100;
        number %= 100;

        if (crore > 0) parts.Add($"{ConvertBelowThousand(crore)} Crore");
        if (lakh > 0) parts.Add($"{ConvertBelowThousand(lakh)} Lakh");
        if (thousand > 0) parts.Add($"{ConvertBelowThousand(thousand)} Thousand");
        if (hundred > 0) parts.Add($"{Units[hundred]} Hundred");
        if (number > 0)
        {
            if (parts.Count > 0) parts.Add("and");
            parts.Add(ConvertBelowThousand(number));
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string ConvertBelowThousand(long number)
    {
        if (number < 20) return Units[number];
        if (number < 100)
        {
            var ten = number / 10;
            var unit = number % 10;
            return unit == 0 ? Tens[ten] : $"{Tens[ten]} {Units[unit]}";
        }

        var hundred = number / 100;
        var rest = number % 100;
        return rest == 0
            ? $"{Units[hundred]} Hundred"
            : $"{Units[hundred]} Hundred {ConvertBelowThousand(rest)}";
    }
}
