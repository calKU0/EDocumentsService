namespace EReturns.Service.Helpers
{
    public static class ReturnsEmailBuilder
    {
        public static string BuildReturnBody(string country)
        {
            if (country == "PL")
            {
                return
                    "Drodzy Państwo," +
                    "<br/><br/>Przesyłamy w załączniku protokół zwrotu towarów." +
                    "<br/>Prosimy o zapoznanie się z treścią dokumentu. W przypadku zaakceptowania zwrotu w dokumencie przez firmę Gąska sp. z o.o. prosimy o wydrukowanie protokołu i dołączenie do przesyłki." +
                    "<br/>Przypominamy, iż korekta zostanie wystawiona wyłącznie na zaakceptowane zwroty." +
                    "<br/>W przypadku pytań prosimy o kontakt z Państwa opiekunem lub działem reklamacji i zwrotów." +
                    "<br/><br/>Pozdrawiamy/Best Regards" +
                    "<br/>Gąska sp. z o.o." +
                    "<br/>Gotkowice 85" +
                    "<br/>32-048 Jerzmanowice" +
                    "<br/>POLAND" +
                    "<br/>NIP: 677-000-03-35";
            }
            else if (country == "UA")
            {
                return "Шановні Пані та Панове,"
                    + "<br/><br/>" + "В файлі пересилаємо протокол повернення товарів."
                    + "<br/>" + "Просимо ознайомитися зі змістом документу. В разі підтвердження повернення в документі фірмою Gąska sp. z o.o. просимо розпечатати протокол і прикріпити до пересилки."
                    + "<br/>" + "Нагадуємо, що поправка буде виставлена виключно на розлянуті та схвалені повернення."
                    + "<br/>" + "В разі питань просимо сконтактуватися з опікуном або відділом рекламацій."
                    + "<br/><br/>" + "Pozdrawiamy/Best Regards"
                    + "<br/>" + "Gąska sp. z o.o."
                    + "<br/>" + "Gotkowice 85"
                    + "<br/>" + "32-048 Jerzmanowice"
                    + "<br/>" + "POLAND"
                    + "<br/>" + "NIP: 677-000-03-35";
            }
            else if (country == "RO")
            {
                return "Bună ziua,"
                    + "<br/><br/>" + "Vă atașez protocolul de retur al mărfurilor."
                    + "<br/>" + "Vă rugăm să consultați documentul. În cazurile de retur acceptate de către compania Gąska sp. z o.o., imprimați documentul și atașați-l."
                    + "<br/>" + "Vă amintim că corecțiile vor fi făcute doar pentru reclamațiile acceptate."
                    + "<br/>" + "Dacă aveți întrebări, contactați departamentul de reclamații sau persoana de contact de pe platforma online."
                    + "<br/><br/>" + "Pozdrawiamy/Best Regards"
                    + "<br/>" + "Gąska sp. z o.o."
                    + "<br/>" + "Gotkowice 85"
                    + "<br/>" + "32-048 Jerzmanowice"
                    + "<br/>" + "POLAND"
                    + "<br/>" + "NIP: 677-000-03-35";
            }
            else if (country == "DE")
            {
                return "Sehr geehrte Damen und Herren"
                    + "<br/><br/>" + "Wir senden in anhang protokoll rücksendung der Waren."
                    + "<br/>" + "Bitte lesen sie den inhalt des dokuments. Wenn sie die rückkehr des dokuments durch Gąska sp. z o.o. bitte das protokoll ausdrucken und an der sendung anbringen."
                    + "<br/>" + "Wir weisen darauf hin, dass die einstellung nur für zugelassene renditen ausgegeben werden."
                    + "<br/>" + "Wenn sie fragen haben, wenden sie sich bitte ihre vertreter oder die kundenbeschwerden und kehrt nennen."
                    + "<br/><br/>" + "Pozdrawiamy/Best Regards"
                    + "<br/>" + "Gąska sp. z o.o."
                    + "<br/>" + "Gotkowice 85"
                    + "<br/>" + "32-048 Jerzmanowice"
                    + "<br/>" + "POLAND"
                    + "<br/>" + "NIP: 677-000-03-35";
            }
            else
            {
                return
                    "Dear Sir or Madame," +
                    "<br/><br/>We send you protocol returning the goods in the attachment." +
                    "<br/>Please read the contents of the document. In case of approved return document by Gąska sp. z o.o. please print out the protocol and attach to the shipment." +
                    "<br/>We remind you that the credit note will be made only for approved returns." +
                    "<br/>If you have questions, please contact your representative or call the complaints and returns department." +
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
