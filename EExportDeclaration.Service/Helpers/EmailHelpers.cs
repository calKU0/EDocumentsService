namespace EExportDeclaration.Service.Helpers
{
    public static class EmailHelpers
    {
        public static string BuildExportDeclarationBody(string country)
        {
            if (country == "PL")
            {
                return
                    "Drodzy Państwo," +
                    "<br/><br/>W związku z zakupami dokonanymi w naszej firmie w poprzednim miesiącu przesyłamy do Państwa dokument potwierdzenia dostawy towaru z terytorium Polski." +
                    "<br/>Zgodnie z obowiązującymi przepisami w Unii Europejskiej prosimy o podpisanie załączonego dokumentu i odesłanie na adres:" +
                    "<br/>platnosci@gaska.com.pl do 7 dni od otrzymania towaru." +
                    "<br/><br/>Pozdrawiamy/Best Regards" +
                    "<br/>Gąska sp. z o.o." +
                    "<br/>Gotkowice 85" +
                    "<br/>32-048 Jerzmanowice" +
                    "<br/>POLAND" +
                    "<br/>NIP: 677-000-03-35";
            }
            else
            {
                return
                    "Dear Sir or Madame," +
                    "<br/><br/>Referring to your last month purchase in our company we are sending to you confirmation delivery goods from the territory of Poland." +
                    "<br/>According to applicable laws in European Union we kindly ask you to make a signature, stamp under the document and send it back to us at our e-mail address:" +
                    "<br/>platnosci@gaska.com.pl not later than 7 days from receipt of this e-mail" +
                    "<br/><br/>Pozdrawiamy/Best Regards" +
                    "<br/>Gąska sp. z o.o." +
                    "<br/>Gotkowice 85" +
                    "<br/>32-048 Jerzmanowice" +
                    "<br/>POLAND" +
                    "<br/>NIP: 677-000-03-35";
            }
        }
    }
}
