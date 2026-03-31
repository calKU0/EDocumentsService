namespace EDocuments.Infrastructure.Helpers
{
    public static class ErrorEmailBuilder
    {
        public static string BuildErrorBodyForRepresentative(string documentName, List<string> emails)
        {
            return $"Dla dokumentu numer: {documentName}, nie została wysłana faktura, " +
                            $"ponieważ podano nieprawidłowy adres e-mail:<br/><br/>" +
                            $"{string.Join("<br/>", emails)}" +
                            $"<br/><br/>Pozdrawiamy<br/>Dział IT";
        }
    }
}
