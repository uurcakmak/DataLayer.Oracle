namespace DataLayer.Oracle.Helpers
{
    public static class CriteriaHelper
    {
        public static string SafeInputParam(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var replace = input.Replace("'", "''");
            replace = replace.Replace("%", "[%]");
            replace = replace.Replace("_", "[_]");
            replace = replace.Replace("?", "[?]");

            return replace;
        }
    }
}
