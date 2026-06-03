namespace Gamestore.Extensions
{
    public static class Extensions
    {
        public static string Capitalize(this string text)
        {
            text = text.Trim();
            return char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();
        }
    }
}
