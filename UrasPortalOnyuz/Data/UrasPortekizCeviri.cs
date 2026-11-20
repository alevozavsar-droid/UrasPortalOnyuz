// ============================================================
// URAS PORTEKİZ RAPORU — İNGİLİZCE → TÜRKÇE ÇEVİRİ (20 Kas 2026)
//   Kaynak Excel'deki (REPORT_PORTUGAL_*.xls) banka / kasa / öngörü açıklamaları serbest İngilizce metindir (yaklaşık 5.000 farklı satır).
//   Çeviri kalıp + sözlük tabanlıdır: önce özel kalıplar (uzundan kısaya), sonra tek sözcükler. Kaynak metin BÜYÜK HARFLİDİR; çevrilen kısımlar
//   küçük / cümle düzeninde yazılır, böylece özel adlar (müşteri, kişi, firma, fatura no) olduğu gibi büyük harfle kalır ve çeviriden ayırt edilir.
//   Kurallar büyük harfe duyarlıdır: Türkçe karşılıklar küçük harf içerdiğinden sonraki kurallarla yeniden eşleşmez.
//   Ayrıca her hareket bir TÜRE ayrılır (Tur): Müşteri Tahsilatı, Maaş, Vergi, Kira … — özet ve süzgeç için.
//   Asıl İngilizce metin raporda ipucunda ve "Asıl Metin" sütununda saklanır (çeviri hatasına karşı).
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebApplication3.Data
{
    public static class UrasPortekizCeviri
    {
        static readonly (Regex R, string Y)[] Kurallar;
        static readonly Dictionary<string, string> Sozluk;
        static readonly Dictionary<string, string> Onbellek = new Dictionary<string, string>();
        const RegexOptions O = RegexOptions.CultureInvariant | RegexOptions.Compiled;
        static (Regex, string) K(string desen, string yerine) => (new Regex(desen, O), yerine);

        static readonly string[,] Aylar = { { "JANUARY", "Ocak" }, { "FEBRUARY", "Şubat" }, { "MARCH", "Mart" }, { "APRIL", "Nisan" }, { "MAY", "Mayıs" }, { "JUNE", "Haziran" }, { "JULY", "Temmuz" }, { "AUGUST", "Ağustos" }, { "SEPTEMBER", "Eylül" }, { "OCTOBER", "Ekim" }, { "NOVEMBER", "Kasım" }, { "DECEMBER", "Aralık" } };

        static UrasPortekizCeviri()
        {
            var k = new List<(Regex, string)>
            {
                // ---- tahsilat
                K(@"^PAYMENT RECEIVED FROM\s+", "Tahsilat: "), K(@"\bPAYMENTS? RECEIVED FROM\b", "tahsilat:"), K(@"\(RECEIPTS?\s*([^)]*)\)", "(makbuz $1)"), K(@"\bRECEIPTS?\b", "makbuz"),
                // ---- hesaplar arası
                K(@"TRANSFER SERVICE ACCOUNT \(INTERNAL TRANSFER BETWEEN URAS BANK ACCOUNTS\)", "Hesaplar arası transfer (Uras banka hesapları arasında)"),
                K(@"INTERNAL TRANSFER BETWEEN URAS BANK ACCOUNTS", "Uras banka hesapları arasında iç transfer"),
                K(@"TRANSFER FROM PRIVATE TO COMPANY ACCOUNT", "Özel hesaptan şirket hesabına transfer"), K(@"TRANSFER FROM COMPANY TO PRIVATE ACCOUNT", "Şirket hesabından özel hesaba transfer"),
                K(@"\bINSTALMENT URAS KİMYA TR\b", "Taksit: URAS KİMYA TR"), K(@"\bINSTALL?MENT URAS\b", "Taksit: URAS"),
                // ---- öngörü sayfası
                K(@"URAS PT BANK ACCOUNT BALANCE - SAVINGS", "Uras PT banka hesabı bakiyesi - tasarruf"), K(@"URAS PT BANK ACCOUNT BALANCE", "Uras PT banka hesabı bakiyesi"), K(@"MR\.? MURAT PRIVATE? BANK ACCOUNT BALANCE", "Murat Bey özel banka hesabı bakiyesi"),
                K(@"\(LIKE WATER, ELECTRIC, TELECOMMUNICATION, DIESEL, CLEANING, OGS/HGS, TRANSPORT ETC\)", "(su, elektrik, telekomünikasyon, motorin, temizlik, OGS / HGS, nakliye vb.)"),
                K(@"\bGENERAL EXPENSES\b", "Genel giderler"), K(@"AMOUNTS? TO RECEIVE FROM CLIENTS?\s*-?\s*", "Müşterilerden tahsil edilecek tutar - "), K(@"\(SALES MADE\)", "(yapılan satışlar)"),
                K(@"WORKING CAPITAL FOR\s+(\w+) PAYMENTS", "$1 ödemeleri için işletme sermayesi"), K(@"\bPENDING TRANSPORT\b", "bekleyen nakliye"),
                K(@"PROFESSIONAL TRAINING FOR WORKERS - ANN?UAL MANDATORY HOURS BY LAW", "Çalışanlar için mesleki eğitim - yasal zorunlu yıllık saat"),
                K(@"PAYMENT URAS TURKEY\s*-\s*NEW ORDER", "Uras Türkiye ödemesi - yeni sipariş"), K(@"PAYMENT URAS TURKEY\s*-\s*PLAFOND EXCESS", "Uras Türkiye ödemesi - limit aşımı"), K(@"\bPLAFOND EXCESS\b", "limit aşımı"),
                K(@"\bCOMPENSATION INVOICE\b", "Mahsup faturası"), K(@"\bSALARIES\b", "Maaşlar"), K(@"(\d+)\s*%\s*HOLIDAYS? SUBSIDY", "% $1 tatil ikramiyesi"),
                // ---- vergi / SGK
                K(@"FISCAL AUTHORITY INCOME RETENTION SALARY", "Vergi dairesi: maaş gelir vergisi stopajı"), K(@"FISCAL AUTHORITY INCOME RETENTION RENT", "Vergi dairesi: kira gelir vergisi stopajı"), K(@"FISCAL AUTHORITY INCOME RETENTION", "Vergi dairesi: gelir vergisi stopajı"), K(@"\bFISCAL AUTHORITY\b", "Vergi dairesi"),
                K(@"INCOME TAX RETENTION (\w+) RENT", "Kira gelir vergisi stopajı $1"), K(@"\bINCOME TAX RETENTION\b", "Gelir vergisi stopajı"), K(@"\bINCOME RETENTION\b", "gelir vergisi stopajı"),
                K(@"INCOME TAX COMPANY (\d{4})", "Kurumlar gelir vergisi $1"), K(@"TAX UNDER THE EXPENSES WITH CARS AND CLIENTS", "araç ve müşteri giderleri üzerinden vergi"), K(@"WITH PAYMENT IN TERMS", "taksitli ödeme"),
                K(@"\bREVERSE CHARGE CORPORATION TAX\b", "ters ibraz kurumlar vergisi"), K(@"\bCORPORAT(E|ION) TAX\b", "Kurumlar vergisi"), K(@"\bSINGLE CIRCULATION TAX\b", "tek dolaşım vergisi (araç vergisi)"),
                K(@"\bPAYMENT VAT\b", "KDV ödemesi"), K(@"\bVAT\b", "KDV"), K(@"\bWORKERS SOCIAL INSURANCE\b", "Çalışan sosyal sigortası"), K(@"\bSOCIAL SECURITY\b", "Sosyal güvenlik primi (SGK)"),
                K(@"\bPAYMENT COMPENSATION FUNDS?\b", "Tazminat fonu ödemesi"), K(@"\bCOMPENSATION FUNDS?\b", "tazminat fonu"), K(@"PAYMENT SIMPLIFIED BUSINESS INFORMATION", "Basitleştirilmiş işletme bilgisi (IES) ödemesi"),
                K(@"PAYMENT IGAMAOT FINE", "IGAMAOT cezası ödemesi"), K(@"NEW AGREEMENT\s*[-_]\s*PAYMENT IN (\d+) INSTALLMENTS", "yeni anlaşma: $1 taksitte ödeme"), K(@"\bNEW AGREEMENT\b", "yeni anlaşma"),
                K(@"NOTARY FEES - COMMERCIAL REGISTER - PRESENTATION ANNUAL ACCOUNTANCY", "Noter ücreti - ticaret sicili - yıllık hesapların sunumu"), K(@"\bNOTARY FEES?\b", "Noter ücreti"), K(@"\bCOMMERCIAL REGISTER\b", "ticaret sicili"),
                // ---- taksit / dönem
                K(@"(\d+)\s*(?:ST|ND|RD|TH)\s*-\s*(\d+)\s*(?:ST|ND|RD|TH)\s+TERMS?\b", "$1. - $2. dönem"), K(@"(\d+)\s*(?:ST|ND|RD|TH)\s+TERMS?\b", "$1. dönem"),
                K(@"(\d+)\s*(?:ST|ND|RD|TH)\s+INSTALL?MENTS?\s+OF\s+(\d+)", "$1. taksit / $2"), K(@"(\d+)\s*(?:ST|ND|RD|TH)\s+INSTALL?MENTS?\b", "$1. taksit"), K(@"PAYMENT IN (\d+) INSTALL?MENTS", "$1 taksitte ödeme"), K(@"\bINSTALL?MENTS?\b", "taksit"),
                // ---- Uras Türkiye ödemeleri
                K(@"PARTIAL PAYMENT OF YOUR INVOICES?", "Faturanıza kısmi ödeme:"), K(@"FINAL PAYMENT OF YOUR INVOICES?", "Faturanıza son ödeme:"), K(@"PAYMENT OF YOUR INVOICES?", "Faturanızın ödemesi:"),
                K(@"^PARTIAL PAYMENT\b", "Kısmi ödeme:"), K(@"^FINAL PAYMENT\b", "Son ödeme:"), K(@"\bPARTIAL PAYMENT\b", "kısmi ödeme"), K(@"\bFINAL PAYMENT\b", "son ödeme"),
                K(@"\bDATED\s+(\d{1,2}[./]\d{1,2}[./]\d{2,4})", "($1 tarihli)"), K(@"\bDATED\b", "tarihli"),
                K(@"BANK CHARGES FEES \(INTERNATIONAL TRANSFER URAS TR\)", "Banka masrafları (Uras TR uluslararası transferi)"), K(@"\bINTERNATIONAL TRANSFER\b", "uluslararası transfer"),
                K(@"\bBANK CHARGES FEES\b", "Banka masrafları"), K(@"\bBANK CHARGES\b", "banka masrafı"), K(@"\bBANK FEES\b", "banka masrafı"),
                // ---- maaş
                K(@"REMAINING AMOUNT OF ALEXANDRES OFF SALARY \(PAID\)", "ALEXANDRE'nin bordro dışı maaşından kalan tutar (ödendi)"), K(@"REMAINING AMOUNT OF ALEXANDRES OFF SALARY", "ALEXANDRE'nin bordro dışı maaşından kalan tutar"),
                K(@"REMAINING AMOUNT RELATING TO THE PAYMENT OF THE TWO PERSONS THAT HELPED TO UNLOAD THE (\d+)(?:ST|ND|RD|TH) CONTAINER", "$1. konteyneri boşaltmaya yardım eden iki kişinin ödemesinden kalan tutar"),
                K(@"\bREMAINING AMOUNT OF\b", "kalan tutar:"), K(@"\bREMAINING AMOUNT\b", "kalan tutar"),
                K(@"\bSALARY OFF COMPANY\b", "Bordro dışı maaş"), K(@"\bOFF SALARY\b", "bordro dışı maaş"), K(@"\bSALARY\b", "Maaş"),
                K(@"\bCHRISTMAS SUBSIDY\b", "Noel ikramiyesi"), K(@"\bHOLIDAYS? SUBSIDY\b", "Tatil ikramiyesi"), K(@"\bVACATIONS? (ALLOWANCE|SUBSIDY)\b", "Tatil ikramiyesi"), K(@"\bSUBSIDY\b", "ikramiye"), K(@"\bALLOWANCE\b", "ödenek"),
                // ---- kira / bina
                K(@"PAYMENT BUILDING RENT REF\.? MONTH", "Bina kirası ödemesi, ay:"), K(@"\bBUILDING RENT\b", "bina kirası"), K(@"WARE ?HOUSE RENT WARRANTY", "Depo kirası depozitosu"), K(@"WARE ?HOUSE RENT", "Depo kirası"),
                K(@"\((\d+) MONTHS? IN ADVANCE", "($1 ay peşin"), K(@"\bIN ADVANCE\b", "peşin"), K(@"\bREF\.? MONTH\b", "ay:"),
                // ---- nakliye / gümrük
                K(@"PAYMENT TRANSPORT COMPANY\s+", "Nakliye firması ödemesi: "), K(@"EXPENDITURE ON ARRIVAL", "varış masrafları"), K(@"CUSTOMS CLEARANCE", "gümrükleme"), K(@"TRANSPORT TO OUR WAREHOUSE", "depomuza nakliye"),
                K(@"TRANSPORT TO THE CUSTOMERS?", "müşteriye nakliye:"), K(@"\((\d+)\s*(?:ST|ND|RD|TH) CONTAINER\)", "($1. konteyner)"), K(@"(\d+)\s*(?:ST|ND|RD|TH) CONTAINER", "$1. konteyner"), K(@"\bCONTAINERS?\b", "konteyner"),
                K(@"\bPALLETS?\b", "palet"), K(@"\bBOXES\b", "koli"), K(@"\bBOX\b", "koli"), K(@"\bHS CODE EXTRA\b", "ek GTİP kodu"), K(@"IMPORT OF URAS TR SAMPLES DHL EXPENSES", "Uras TR numune ithalatı DHL giderleri"), K(@"\bSAMPLES\b", "numune"),
                K(@"\bTRANSPORT COMPANY\b", "nakliye firması"), K(@"\bCUSTOMS\b", "gümrük"), K(@"\bTRANSPORT\b", "nakliye"), K(@"\bFREIGHT\b", "navlun"), K(@"\bDELIVERY\b", "teslimat"),
                // ---- araç / yakıt
                K(@"TOLL HIGHWAY CHARGES LIKE OGS/HGS", "Otoyol geçiş ücreti (OGS / HGS benzeri)"), K(@"\bTOLL HIGHWAY CHARGES\b", "Otoyol geçiş ücreti"),
                K(@"FIXED FUND FOR FUEL EXPENSES (\w+) \(DIESEL\)", "Yakıt giderleri için sabit avans: $1 (motorin)"), K(@"EXPENSES DIESEL\s*-\s*", "Motorin gideri - "), K(@"EXPENSES (\w+) \(DIESEL\)", "$1 giderleri (motorin)"),
                K(@"PAYMENT COMPANY CAR \(PLATE ([^)]+)\)", "Şirket aracı ödemesi (plaka $1)"), K(@"PAYMENT INSURANCE (\d+) CARS \(PLATES? ([^)]+)\)", "$1 aracın sigorta ödemesi (plaka $2)"),
                K(@"(\d+) NEW TIRES FOR THE COMPANY CAR", "şirket aracı için $1 yeni lastik:"), K(@"FOR THE CARS? / PLATES?", "- araç / plaka"), K(@"\bCAR ?WASH\b", "araç yıkama"), K(@"\bCOMPANY CARS?\b", "şirket aracı"), K(@"\bPLATES?\b", "plaka"), K(@"\bCARS\b", "araçlar"), K(@"\bCAR\b", "araç"),
                K(@"\bDIESEL\b", "motorin"), K(@"\bFUEL\b", "yakıt"), K(@"\bADBLUE\b", "AdBlue"), K(@"\bTIRES?\b", "lastik"), K(@"\bBRAKE PADS\b", "fren balatası"), K(@"\bMECHANIC\b", "tamirci"), K(@"\bINSPECTION\b", "muayene"), K(@"\bCAR WASH\b", "araç yıkama"),
                // ---- fatura / hizmet sağlayıcılar
                K(@"MONTHLY PAYMENT SECURITAS", "Securitas aylık ödemesi"), K(@"PAYMENT INSTALATION SECURITAS DIRECT", "Securitas Direct kurulum ödemesi"),
                K(@"PAYMENT VODAFONE \(TELEPHONE \+ INTERNET - PERIOD", "Vodafone ödemesi (telefon + internet - dönem"), K(@"\bTELEPHONE \+ INTERNET\b", "telefon + internet"), K(@"\bPERIOD\b", "dönem"),
                K(@"PAYMENT EDP \(ENERGY\)", "EDP ödemesi (elektrik)"), K(@"\(ENERGY\)", "(elektrik)"), K(@"\bENERGY\b", "elektrik"), K(@"\bELECTRICITY\b", "elektrik"), K(@"\bELECTRIC\b", "elektrik"), K(@"\bWATER\b", "su"), K(@"\bGAS\b", "gaz"),
                K(@"PAYMENT GRENKE - MONTHLY PRINTER RENTAL", "Grenke ödemesi - aylık yazıcı kirası"), K(@"PAYMENT GRENKE - INSURANCE FOR", "Grenke ödemesi - sigorta"),
                K(@"PAYMENT XDOC PRINTERS RENT \+ MAINTAINANCE \+ PRINTS", "XDOC ödemesi: yazıcı kirası + bakım + baskılar"), K(@"\bPRINTERS? RENT(AL)?\b", "yazıcı kirası"), K(@"\bMAINT[AE]I?NANCE\b", "bakım"), K(@"\bPRINTS\b", "baskılar"), K(@"\bPRINTERS?\b", "yazıcı"),
                K(@"PAYMENT NOSSOPODIUM \(COFFEE, SUGAR, SPOONS, CUPS\)", "Nossopodium ödemesi (kahve, şeker, kaşık, bardak)"), K(@"\bCOFFEE\b", "kahve"), K(@"\bSUGAR\b", "şeker"), K(@"\bSPOONS\b", "kaşık"), K(@"\bCUPS\b", "bardak"),
                K(@"PAYMENT CLEANING SERVICE", "Temizlik hizmeti ödemesi"), K(@"\bCLEANING SERVICES?\b", "temizlik hizmeti"), K(@"\bCLEANING\b", "temizlik"),
                K(@"PAYMENT PROVISION OF SERVICES OF SAFETY AND HYGIENE AT WORK BY\s+", "İş güvenliği ve hijyen hizmeti ödemesi: "), K(@"SAFETY AND HYGIENE AT WORK", "iş güvenliği ve hijyen"), K(@"\bMEDICAL CONSULTATIONS?\b", "sağlık muayenesi"),
                K(@"WORKERS ACCIDENTS AT WORK INSURANCE", "Çalışan iş kazası sigortası"), K(@"ACCIDENTS AT WORK", "iş kazası"),
                K(@"ASSISTANCE CONTRACT FOR THE MONTHS", "destek sözleşmesi, aylar:"), K(@"\bASSISTANCE CONTRACT\b", "destek sözleşmesi"), K(@"SOFTWARE MICROSOFT OFFICE (\d+) - BUSINESS ANNUAL", "Microsoft Office $1 yazılımı - yıllık iş lisansı"),
                K(@"\(CONSULTING SERVICES REACH\)", "(REACH danışmanlık hizmeti)"), K(@"\bCONSULTING SERVICES\b", "danışmanlık hizmeti"), K(@"\bMANAGEMENT CONSULTANCY\b", "Yönetim danışmanlığı"), K(@"\bCONSULTANCY\b", "danışmanlık"),
                K(@"ACCOUNTANCE FEES\s*/\s*", "Muhasebe ücreti / "), K(@"\bACCOUNTAN(CE|CY) FEES\b", "Muhasebe ücreti"), K(@"DIGITAL ARCHIVE SERVICES", "dijital arşiv hizmeti"), K(@"\bACCOUNTAN(CE|CY) SOFTWARE\b", "muhasebe yazılımı"), K(@"\bACCOUNTANT\b", "muhasebeci"), K(@"\bACCOUNTAN(CE|CY)\b", "muhasebe"),
                K(@"LAWYER HONORARY COSTS", "Avukatlık ücreti"), K(@"\bLAWYER\b", "avukat"),
                K(@"PARTIAL PAYMENT OF OFFICE FURNITURES", "Ofis mobilyası kısmi ödemesi"), K(@"PARTIAL PAYMENT OF COMPUTERS / SOFTWARE LICENSES / ACCOUNTANCE SOFTWARE", "Bilgisayar / yazılım lisansı / muhasebe yazılımı kısmi ödemesi"),
                K(@"\((\d+)\s*% OF (\d+)\s*%\)", "(% $2'ün % $1'i)"), K(@"\bOFFICE FURNITURES?\b", "ofis mobilyası"), K(@"\bSOFTWARE LICENSES?\b", "yazılım lisansı"), K(@"\bSOFTWARE\b", "yazılım"), K(@"\bCOMPUTERS?\b", "bilgisayar"), K(@"\bLICENSES?\b", "lisans"),
                K(@"FORKLIFT \+ TRANSPALET", "Forklift + transpalet"),
                // ---- nakit çekim / ziyaret
                K(@"ATM WITHDRAW TO PAY SEVGI'S EXPENSES DURING URAS PORTUGAL VISIT \(FLIGHT\+HOTELS\)", "ATM'den çekim: SEVGI'nin Uras Portekiz ziyareti giderleri (uçak + otel)"),
                K(@"REMAINING AMOUNT OF WITHDRAW TO PAY SEVGI'S EXPENSES DURING URAS PORTUGAL VISIT \(FLIGHT\+HOTELS\)", "SEVGI'nin Uras Portekiz ziyareti giderleri için çekilen tutardan kalan (uçak + otel)"),
                K(@"WITHDRAW TO PAY SEVGI'S EXPENSES DURING URAS PORTUGAL VISIT \(FLIGHT\+HOTELS\)", "Çekim: SEVGI'nin Uras Portekiz ziyareti giderleri (uçak + otel)"),
                K(@"\bATM WITHDRAW(AL)?\b", "ATM'den çekim"), K(@"\bWITHDRAW(AL)?\b", "çekim"), K(@"\bFLIGHTS?\b", "uçak"), K(@"\bHOTELS?\b", "otel"), K(@"\bVISIT\b", "ziyaret"), K(@"\bDURING\b", "sırasında"),
                K(@"AMOUNT GIVEN TO (\S+) TO PAY THE", "$1 firmasına verilen tutar, ödeme için:"), K(@"\bSOLIDARITY DONATION\b", "Dayanışma bağışı"), K(@"\bDONATION\b", "bağış"),
                // ---- ikinci tur: sık geçen öbür kalıplar (gider ayrıntıları, araç bakımı, ofis, hediye, düzeltmeler)
                K(@"\bRESTAURANT WITH CUSTOMERS\b", "müşterilerle restoran"), K(@"\bLITTLE GIFT FOR\b", "küçük hediye:"),
                K(@"\bADDITIONAL AMOUNT\b", "ek tutar"), K(@"\bOIL CHANGE\b", "yağ değişimi"), K(@"\bFILTER CHANGE\b", "filtre değişimi"), K(@"\bMAINTENANCE SERVICE\b", "bakım servisi"), K(@"\bMECHANIC SERVICE\b", "tamir servisi"),
                K(@"\bALTERNATOR BELT\b", "alternatör kayışı"), K(@"\bCLUTCH KIT\b", "debriyaj seti"), K(@"\bCLUTCH LININGS\b", "debriyaj balatası"), K(@"\bSHOCK ABSORBERS?\b", "amortisör"), K(@"\bPERIODIC INSPECTION\b", "periyodik muayene"), K(@"\bMISSING INSPECTION\b", "yapılmamış muayene"),
                K(@"\bADMINISTRATIVE OFFENSE\b", "idari para cezası"), K(@"\bHAND TRUCK\b", "el arabası"), K(@"\bBY TRUCK\b", "kamyonla"), K(@"\(LIKE OGS/HGS\)", "(OGS / HGS benzeri)"),
                K(@"INVOICE FOR LITTLE GIFTS FOR CUSTOMERS", "Müşterilere küçük hediyeler faturası"), K(@"\bLITTLE GIFTS FOR CUSTOMERS\b", "müşterilere küçük hediyeler"), K(@"\bGIFTS? FOR CUSTOMERS\b", "müşterilere hediye"), K(@"\bCHRISTMAS GIFTS?\b", "Noel hediyesi"), K(@"\bEASTER GIFTS?\b", "Paskalya hediyesi"),
                K(@"\bMATERIALS? FOR THE OFFICE\b", "ofis malzemesi"), K(@"\bOFFICE PAPER\b", "ofis kâğıdı"), K(@"\bTOILET PAPER\b", "tuvalet kâğıdı"), K(@"\bREAM OF PAPER\b", "top kâğıt"), K(@"\bPERMANENT MARKER\b", "kalıcı kalem"), K(@"\bARCHIVE FOLDERS?\b", "arşiv klasörü"),
                K(@"\bPORTABLE PRINTING CALCULATOR\b", "şeritli hesap makinesi"), K(@"\bLIQUID SOAP\b", "sıvı sabun"), K(@"\bFIRE EXTINGUISHERS?\b", "yangın söndürücü"), K(@"\bPLASTIC CUPS\b", "plastik bardak"), K(@"\bWINE BAGS\b", "şarap poşeti"), K(@"\bWINE BOXES\b", "şarap kutusu"), K(@"\bWINE BOTTLES\b", "şarap şişesi"),
                K(@"DEPOSIT OF PLEDGED VALUES", "Haczedilen tutarların yatırılması"), K(@"DEPOSIT OF THE ATM MADE BY MISTAKE", "Yanlışlıkla ATM'den yapılan yatırma"), K(@"\bBY MISTAKE\b", "yanlışlıkla"), K(@"\bPAYMENT DONE\b", "yapılan ödeme"),
                K(@"PAYMENT IRS\s*-\s*BUSINESS AND PROFESSIONAL", "IRS ödemesi - ticari ve mesleki kazanç stopajı"), K(@"\bBUSINESS AND PROFESSIONAL\b", "ticari ve mesleki kazanç"),
                K(@"(\d+/\d+) REDUCTION DUE TO COVID-19 PANDEMIC", "COVID-19 salgını nedeniyle $1 indirim"), K(@"\bDUE TO\b", "nedeniyle:"), K(@"\bREDUCTION\b", "indirim"), K(@"\bREGARDING\b", "ile ilgili:"), K(@"\bREFERRING TO\b", "ile ilgili:"),
                K(@"UPDATED IRS WITHHOLDING RATES", "güncellenen IRS stopaj oranları"), K(@"\bRETIFICATION\b", "düzeltme"), K(@"\bWITHHOLDING\b", "stopaj"),
                K(@"SME \(SMALL AND MEDIUM-SIZED ENTERPRISE\) CERTIFICATE", "KOBİ (küçük ve orta ölçekli işletme) sertifikası"), K(@"SME \(SMALL AND MEDIUM-SIZED ENTERPRISE\)", "KOBİ (küçük ve orta ölçekli işletme)"), K(@"\bSME\b", "KOBİ"),
                K(@"TO PAY THE (\w+) PERSONS THAT HELPED (?:TO UNLOAD|UNLOADING) THE", "- boşaltmaya yardım eden $1 kişiye ödeme:"), K(@"\bHALF MONTH\b", "yarım ay"), K(@"\bREMAINING PART\b", "kalan kısım"), K(@"\(REMAINING\)", "(kalan)"), K(@"\bLAST PAYMENT\b", "Son ödeme"),
                K(@"\bCOMMERCIAL INVOICE NR\.?", "ticari fatura no"), K(@"\bFOR PRODUCTS OF YOUR INVOICE\b", "faturanızdaki ürünler için:"), K(@"\bYOUR INVOICE\b", "faturanız"), K(@"\bOUR BANK ACCOUNT\b", "banka hesabımız"), K(@"\bMR\.\s*", "Bay "),
                K(@"\bTO PAY\b", "- ödeme için:"), K(@"\bAIR ?FREIGHT\b", "hava kargo"), K(@"\bREGISTERED LETTER\b", "taahhütlü mektup"), K(@"\bOFFICE\b", "ofis"),
                // ---- genel
                K(@"^MONTHLY PAYMENT\b", "Aylık ödeme:"), K(@"\bMONTHLY PAYMENT\b", "aylık ödeme"), K(@"^PAYMENT\s+", "Ödeme: "), K(@"\bPAYMENTS?\b", "ödeme"),
                K(@"\bFOR THE MONTHS?\b", "- ay:"), K(@"\(MONTH\s+", "(ay: "), K(@"\bMONTHS\b", "ay"), K(@"\bMONTHLY\b", "aylık"), K(@"\bMONTH\b", "ay"), K(@"\bANN?UAL\b", "yıllık"), K(@"\bYEAR\b", "yıl"),
                K(@"\bINVOICES\b", "faturalar"), K(@"\bINVOICE\b", "fatura"), K(@"\bEXPENSES\b", "giderler"), K(@"\bEXPENSE\b", "gider"), K(@"\bFEES?\b", "ücret"), K(@"\bCHARGES\b", "masraflar"), K(@"\bCOSTS?\b", "masraf"),
                K(@"\bRENTAL\b", "kira"), K(@"\bRENTS?\b", "kira"), K(@"\bWAREHOUSE\b", "depo"), K(@"\bINSURANCE\b", "sigorta"), K(@"\bFINE\b", "ceza"), K(@"\bTAX(ES)?\b", "vergi"),
                K(@"\bBANK ACCOUNTS?\b", "banka hesabı"), K(@"\bACCOUNTS?\b", "hesap"), K(@"\bBALANCE\b", "bakiye"), K(@"\bTRANSFER\b", "transfer"), K(@"\bAMOUNTS?\b", "tutar"),
                K(@"\bCUSTOMERS\b", "müşteriler"), K(@"\bCUSTOMER\b", "müşteri"), K(@"\bCLIENTS?\b", "müşteri"), K(@"\bSUPPLIERS?\b", "tedarikçi"), K(@"\bCOMPANY\b", "şirket"), K(@"\bPRIVATE\b", "özel"), K(@"\bWORKERS\b", "çalışanlar"), K(@"\bWORKER\b", "çalışan"),
                K(@"\bRESTAURANT\b", "restoran"), K(@"\bLUNCH\b", "öğle yemeği"), K(@"\bDINNER\b", "akşam yemeği"), K(@"\bMEALS?\b", "yemek"),
                K(@"\bOFFICE MATERIALS?\b", "ofis malzemesi"), K(@"\bMATERIALS?\b", "malzeme"), K(@"\bPAPER\b", "kâğıt"), K(@"\bLABELS\b", "etiket"), K(@"\bTOOLS\b", "alet"), K(@"\bGLOVES\b", "eldiven"), K(@"\bSOAP\b", "sabun"), K(@"\bTOWELS\b", "havlu"), K(@"\bBOTTLES?\b", "şişe"),
                K(@"\bTRAINING\b", "eğitim"), K(@"\bTRANSLAT(ION|OR)\b", "çeviri"), K(@"\bCERTIFICATE\b", "sertifika"), K(@"\bCONTRACT\b", "sözleşme"), K(@"\bAGREEMENT\b", "anlaşma"), K(@"\bSERVICES?\b", "hizmet"),
                K(@"\bPURCHASE\b", "satın alma"), K(@"\bREPLACEMENT\b", "değişim"), K(@"\bREPAIR\b", "tamir"), K(@"\bRETURN\b", "iade"), K(@"\bREFUND\b", "iade"), K(@"\bDEPOSIT\b", "yatırma"), K(@"\bCREDIT NOTE\b", "alacak dekontu"),
                K(@"\bNEW ORDER\b", "yeni sipariş"), K(@"\bORDER\b", "sipariş"), K(@"\bSALES\b", "satışlar"), K(@"\bIMPORT\b", "ithalat"), K(@"\bEXPORT\b", "ihracat"), K(@"\bSPAIN\b", "İspanya"), K(@"\bLISBON\b", "Lizbon"), K(@"\bTURKEY\b", "Türkiye"), K(@"\bPORTUGAL\b", "Portekiz"),
                K(@"\bPAID\b", "ödendi"), K(@"\bPAY\b", "ödeme"), K(@"\bNEW\b", "yeni"), K(@"\bEXTRA\b", "ek"), K(@"\bTOTAL\b", "toplam"), K(@"\bPART\b", "kısım"), K(@"\bETC\b\.?", "vb."),
                K(@"\bAND\b", "ve"), K(@"\bWITH\b", "ile"), K(@"\bFROM\b", "-"), K(@"\bFOR\b", "-"), K(@"\bOF\b", "-"), K(@"\bTHE\b", ""), K(@"\bTO\b", "→"), K(@"\bBY\b", "-"), K(@"\bAT\b", "-"), K(@"\bIN\b", "-"), K(@"\bON\b", "-"), K(@"\bN/A\b", "—")
            };
            for (int i = 0; i < Aylar.GetLength(0); i++) k.Add(K(@"\b" + Aylar[i, 0] + @"\b", Aylar[i, 1]));
            Kurallar = k.ToArray();
            // tek sözcük sözlüğü: kurallardan sonra hâlâ büyük harfli kalan İngilizce sözcükler (özel adlar burada yoktur, olduğu gibi kalır)
            Sozluk = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ADDITIONAL"] = "ek", ["CHANGE"] = "değişim", ["OIL"] = "yağ", ["FILTER"] = "filtre", ["GIFTS"] = "hediye", ["GIFT"] = "hediye", ["CHOCOLATES"] = "çikolata", ["WINE"] = "şarap", ["BAGS"] = "poşet", ["TOILET"] = "tuvalet",
                ["ENVELOPES"] = "zarf", ["PENS"] = "kalem", ["PEN"] = "kalem", ["PORTFOLIO"] = "dosya", ["MARKER"] = "kalem", ["COOKIES"] = "kurabiye", ["FOLDERS"] = "klasör", ["FOLDER"] = "klasör", ["ARCHIVE"] = "arşiv", ["REAM"] = "top", ["CALCULATOR"] = "hesap makinesi",
                ["CHRISTMAS"] = "Noel", ["EASTER"] = "Paskalya", ["PLASTIC"] = "plastik", ["VACATION"] = "tatil", ["VACATIONS"] = "tatil", ["HOLIDAYS"] = "tatil", ["BUSINESS"] = "iş", ["PROFESSIONAL"] = "mesleki", ["MISTAKE"] = "hata", ["MISTAKEN"] = "hatalı", ["MADE"] = "yapılan", ["DONE"] = "yapılan",
                ["PANDEMIC"] = "salgın", ["UPDATED"] = "güncellenen", ["RATES"] = "oranlar", ["SMALL"] = "küçük", ["MEDIUM"] = "orta", ["SIZED"] = "ölçekli", ["ENTERPRISE"] = "işletme", ["LIKE"] = "gibi", ["MISSING"] = "eksik", ["PERIODIC"] = "periyodik",
                ["PERSONS"] = "kişi", ["THAT"] = "", ["HELPED"] = "yardım eden", ["UNLOAD"] = "boşaltma", ["UNLOADING"] = "boşaltma", ["TWO"] = "iki", ["THREE"] = "üç", ["CREDIT"] = "alacak / iade", ["HALF"] = "yarım", ["SEVERAL"] = "çeşitli", ["REMAINING"] = "kalan",
                ["LAST"] = "son", ["OUR"] = "bizim", ["YOUR"] = "sizin", ["HER"] = "onun", ["PRODUCTS"] = "ürünler", ["TRUCK"] = "kamyon", ["WASH"] = "yıkama", ["CARWASH"] = "araç yıkama", ["CLUTCH"] = "debriyaj", ["LININGS"] = "balata", ["BRAKE"] = "fren", ["PADS"] = "balata",
                ["PUMP"] = "pompa", ["BATTERY"] = "akü", ["BATTERIES"] = "pil", ["WIPER"] = "silecek", ["FRONT"] = "ön", ["BACK"] = "arka", ["KIT"] = "set", ["SET"] = "set", ["SETS"] = "set", ["ORIGINAL"] = "orijinal", ["LIQUID"] = "sıvı", ["HAND"] = "el", ["ALCOHOL"] = "alkol",
                ["DISINFECTANT"] = "dezenfektan", ["TESTS"] = "test", ["TEST"] = "test", ["LAB"] = "laboratuvar", ["QUALITY"] = "kalite", ["COLOR"] = "renk", ["BATCH"] = "parti", ["FORMALDEHYDE"] = "formaldehit", ["DYES"] = "boya", ["CLIPS"] = "ataş", ["CARDS"] = "kart",
                ["TAPE"] = "bant", ["GLUE"] = "yapıştırıcı", ["SCISSORS"] = "makas", ["PENCILS"] = "kurşun kalem", ["STAMPS"] = "pul", ["LETTER"] = "mektup", ["REGISTERED"] = "taahhütlü", ["MAIL"] = "posta", ["POST"] = "posta", ["TICKET"] = "bilet", ["PARKING"] = "otopark",
                ["TRAVELS"] = "seyahat", ["FLOWERS"] = "çiçek", ["BOUQUET"] = "buket", ["SWEETS"] = "tatlı", ["CAKES"] = "pasta", ["PRINT"] = "baskı", ["PRINTING"] = "baskı", ["REPORT"] = "rapor", ["DOCUMENTATION"] = "dokümantasyon", ["REGISTRATION"] = "kayıt",
                ["POLICY"] = "poliçe", ["COURT"] = "mahkeme", ["LABOUR"] = "işçilik", ["WHITE"] = "beyaz", ["RED"] = "kırmızı", ["GREEN"] = "yeşil", ["CORPORATE"] = "kurumsal", ["COMMERCIAL"] = "ticari", ["ADMINISTRATIVE"] = "idari", ["OFFENSE"] = "ceza", ["YEARS"] = "yıl",
                ["MINUTES"] = "dakika", ["UNTIL"] = "kadar", ["REVERSAL"] = "iptal", ["UNDUE"] = "haksız", ["PROCEEDED"] = "yapıldı", ["RELATING"] = "ilişkin", ["DUE"] = "vadesi gelen", ["CODES"] = "kodlar", ["PACKING"] = "ambalaj", ["FIXING"] = "tamir", ["DELIVER"] = "teslim",
                ["DUTY"] = "vergi", ["COURTESY"] = "ikram", ["PROFORMA"] = "proforma", ["ALARM"] = "alarm", ["KEY"] = "anahtar", ["MAGNETIC"] = "manyetik", ["ACTIVE"] = "aktif", ["PACKAGES"] = "paket", ["VACUUM"] = "süpürge", ["DRILL"] = "matkap", ["SCREWDRIVER"] = "tornavida",
                ["HOSE"] = "hortum", ["CHARGER"] = "şarj cihazı", ["SCALE"] = "terazi", ["FILM"] = "film", ["STRETCH"] = "streç", ["IRON"] = "demir", ["SAW"] = "testere", ["BOOK"] = "kitap", ["WINDSHIELD"] = "ön cam", ["FLUID"] = "sıvısı", ["DILUENT"] = "tiner", ["SPATULA"] = "spatula",
                ["JERRYCAN"] = "bidon", ["PUNCTURE"] = "lastik patlağı", ["RAIL"] = "tren", ["PORTABLE"] = "taşınabilir", ["MANDATORY"] = "zorunlu", ["TRAININGS"] = "eğitimler", ["SYNTHETIC"] = "sentetik", ["FASTNESS"] = "haslık", ["SQUARE"] = "kare", ["OFF"] = "dışı", ["LITTLE"] = "küçük", ["BABY"] = "bebek"
            };
        }

        /// <summary>İngilizce açıklamanın Türkçesi; çevrilemeyen özel adlar olduğu gibi kalır.</summary>
        public static string Cevir(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            lock (Onbellek) if (Onbellek.TryGetValue(s, out var hazir)) return hazir;
            string t = Regex.Replace(s.Trim(), @"\s+", " ");
            // tahsilat: müşteri adı özel addır, çevrilmez ("NPB COMPANY" olduğu gibi kalır); yalnız kalan kısım (fatura / makbuz) çevrilir
            var th = Regex.Match(t, @"^PAYMENTS? RECEIVED FROM\s+(.+?)(\s*-\s*(?:FTS?|FAC|INVOICE|NC|ND)(?![A-Z]).*)?$");
            if (th.Success)
            {
                string kalanKisim = th.Groups[2].Success ? th.Groups[2].Value : "";
                string sonuc = "Tahsilat: " + th.Groups[1].Value.Trim() + (kalanKisim == "" ? "" : " - " + CevirIc(Regex.Replace(kalanKisim, @"^\s*-\s*", "")));
                lock (Onbellek) Onbellek[s] = sonuc; return sonuc;
            }
            t = CevirIc(t);
            if (t.Length > 0 && char.IsLower(t[0])) t = char.ToUpper(t[0], new System.Globalization.CultureInfo("tr-TR")) + t.Substring(1);
            lock (Onbellek) Onbellek[s] = t;
            return t;
        }
        static string CevirIc(string t)
        {
            t = Regex.Replace(t, @"(?<=[A-ZÀ-Ý])_(?=[A-ZÀ-Ý])", " - ");   // "VACATION ALLOWANCE_JOÃO" → sözcük sınırı (makbuz no 495_2026 gibi rakamlı olanlara dokunulmaz)
            foreach (var (r, y) in Kurallar) t = r.Replace(t, y);
            t = Regex.Replace(t, @"(?<![A-Za-zÀ-ÿİıŞşĞğÇçÖöÜü'])[A-Z]{2,}(?![A-Za-zÀ-ÿİıŞşĞğÇçÖöÜü'])", m => Sozluk.TryGetValue(m.Value, out var y) ? y : m.Value);
            t = Regex.Replace(t, @"\s+", " "); t = Regex.Replace(t, @"(\s-){2,}", " -"); t = Regex.Replace(t, @"\(\s+", "("); t = Regex.Replace(t, @"\s+\)", ")"); t = Regex.Replace(t, @":\s*-\s*", ": ").Trim(' ', '-');
            return t;
        }

        /// <summary>Satış vadesi: "30 DAYS" → "30 Gün", "PP" → "Peşin".</summary>
        public static string Vade(string v)
        {
            v = Regex.Replace((v ?? "").Trim(), @"\s+", " "); if (v == "") return "";
            var m = Regex.Match(v, @"^(\d+)\s*DAYS?$", RegexOptions.IgnoreCase); if (m.Success) return m.Groups[1].Value + " Gün";
            if (v.Equals("PP", StringComparison.OrdinalIgnoreCase)) return "Peşin";
            return Cevir(v);
        }

        /// <summary>Gider / hareket KALEMİ (20 Kas 2026, kullanıcı: "rakama tıkladığımda maaş mı HGS mi benzin mi kalem kalem görebileyim"): Tur'dan daha ince ayrım — Aylık Giderler sekmesinin satırları ve tıklama detayı.
        /// Sıra önemlidir (ilk uyan kazanır): banka masrafı Uras TR transferinden, stopaj maaştan önce gelir. Karışık masraf beyanları (ör. "EXPENSES MANUEL (RESTAURANT+DIESEL …)") içindeki yakıta göre "Yakıt" sayılır.</summary>
        public static string Kalem(string s, bool giris)
        {
            string t = (s ?? "").ToUpperInvariant();
            bool V(params string[] a) => a.Any(x => t.Contains(x)); bool W(string kelime) => Regex.IsMatch(t, @"\b" + kelime + @"\b");
            if (V("PAYMENT RECEIVED FROM", "AMOUNTS TO RECEIVE", "AMOUNT TO RECEIVE")) return "Müşteri Tahsilatı";
            if (V("INTERNAL TRANSFER BETWEEN", "TRANSFER FROM PRIVATE", "TRANSFER FROM COMPANY", "BANK ACCOUNT BALANCE")) return "Hesaplar Arası Transfer";
            // Uras Türkiye'ye mal ödemesi satırlarının sonunda "+ BANK CHARGES (€ …)" yazar → önce mal bedeli; yalnız masraf satırı ("BANK CHARGES FEES …" ile başlar) banka masrafıdır
            if (t.TrimStart().StartsWith("BANK CHARGES") || t.TrimStart().StartsWith("BANK FEES")) return "Banka Masrafı";
            if (V("INSTALMENT URAS", "INSTALLMENT URAS")) return "Uras Türkiye'den Gelen";
            if (V("IHR", "URAS TR", "URAS TURKEY", "URAS KİMYA", "URAS KIMYA", "YOUR INVOICE", "YOUR COMMERCIAL INVOICE") && !V("SAMPLES")) return "Uras Türkiye Mal Bedeli";
            if (V("DRN GLOBAL", "PROFORMA INVOICE")) return "Mal Alımı (DRN Global ve Diğer)";
            if (V("PLEDGED VALUES")) return "Haciz Kesintisi Yatırma";
            if (V("QUALITY LAB", "BATCH TESTS", "FASTNESS", "CITEVE")) return "Laboratuvar ve Test";
            if (V("FIRE EXTINGUISH", "FIREFIGHTING", "SURVEILLANCE", "CAMERA")) return "Güvenlik";
            if (V("RETENTION", "IRS-", "IRS -", "WITHHOLDING")) return "Gelir Vergisi Stopajı";
            if (V("SUBSIDY", "ALLOWANCE")) return "İkramiye (Noel / Tatil)";
            if (V("SALARY", "SALARIES")) return "Maaş";
            if (V("SOCIAL SECURITY", "SOCIAL INSURANCE")) return "Sosyal Güvenlik (SGK)";
            if (V("COMPENSATION FUND")) return "Tazminat Fonu";
            if (V("IGAMAOT", " FINE", "OFFENSE")) return "Ceza";
            if (V("CIRCULATION TAX")) return "Araç Vergisi";
            if (V("INCOME TAX COMPANY", "CORPORATE TAX", "CORPORATION TAX")) return "Kurumlar Vergisi";
            if (W("VAT")) return "KDV";   // sözcük olarak: "INNOVATION" içindeki VAT sayılmaz
            if (V("TOLL HIGHWAY")) return "Otoyol (OGS / HGS)";
            if (V("DIESEL", "FUEL", "ADBLUE")) return "Yakıt ve Masraf Beyanları";   // tek başına motorin satırları + içinde yakıt geçen karışık masraf beyanları (restoran + motorin + AdBlue …); ayrıntı tıklayınca açıklamada
            if (V("VIA VERDE", "OGS/HGS")) return "Otoyol (OGS / HGS)";
            if (V("TRANSPORT", "CUSTOMS", "DHL", "CONTAINER", "PALLET", "LOGISTICS", "FREIGHT", "LUSOCARGO", "GARLAND")) return "Nakliye ve Gümrük";
            if (W("RENT") && !V("PRINTER", "GRENKE", "XDOC")) return "Kira";
            if (V("COMPANY CAR", " CARS", "PLATE", "TIRES", "PEUGEOT", "CITROEN", "MECHANIC", "INSPECTION", "PNEUS", "AUTO ", "CAR WASH", "CARWASH")) return "Araç (Bakım, Sigorta, Alım)";
            if (V("ACCOUNTAN")) return "Muhasebe";
            if (V("CONSULT", "REACH", "INNOVATION", "INOVATION", "LAWYER", "NOTARY", "TRANSLAT", "BUSINESS INFORMATION", "TAX")) return V("TAX") && !V("CONSULT", "LAWYER", "NOTARY") ? "Diğer Vergi ve Harç" : "Danışmanlık ve Hukuk";
            if (V("EDP", "ENERGY", "ELECTRIC")) return "Elektrik";
            if (V("VODAFONE", "TELEPHONE", "INTERNET", "TELECOM")) return "Telefon ve İnternet";
            if (V("SECURITAS", "ALARM")) return "Güvenlik";
            if (V("CLEANING")) return "Temizlik";
            if (V("GRENKE", "XDOC", "PRINTER")) return "Yazıcı Kirası ve Baskı";
            if (V("AMBIGLOBAL", "SAFETY AND HYGIENE", "MEDICAL", "TRAINING", "ACCIDENTS AT WORK")) return "İş Güvenliği ve Sağlık";
            if (V("INSURANCE")) return "Sigorta";
            if (V("SOFTWARE", "VNI", "COMPUTER", "MICROSOFT", "LICENSE")) return "Yazılım ve Bilgisayar";
            if (V("WATER")) return "Su";
            if (V("NOSSOPODIUM", "COFFEE", "OFFICE", "MAKRO", "STAPLES", "CONTINENTE", "FURNITURE", "FORKLIFT")) return "Ofis, Mutfak ve Demirbaş";
            if (V("WITHDRAW", "RESTAURANT", "HOTEL", "FLIGHT", "LUNCH", "GIFT", "DONATION")) return "Seyahat, Temsil ve Hediye";
            if (V("GENERAL EXPENSES", "EXPENSES")) return "Genel ve Personel Giderleri";
            if (V("BANK CHARGES", "BANK FEES")) return "Banka Masrafı";   // en sonda: başka kaleme girmeyen satırda yalnız masraf kalmışsa
            return giris ? "Diğer Giriş" : "Diğer Gider";
        }

        /// <summary>Hareket türü (özet ve süzgeç için): asıl İngilizce metinden.</summary>
        public static string Tur(string s, bool giris)
        {
            string t = (s ?? "").ToUpperInvariant();
            bool V(params string[] a) => a.Any(x => t.Contains(x)); bool W(string kelime) => Regex.IsMatch(t, @"\b" + kelime + @"\b");
            if (V("PAYMENT RECEIVED FROM", "AMOUNTS TO RECEIVE", "AMOUNT TO RECEIVE")) return "Müşteri Tahsilatı";
            if (V("INTERNAL TRANSFER BETWEEN", "TRANSFER FROM PRIVATE", "TRANSFER FROM COMPANY", "BANK ACCOUNT BALANCE", "SAVINGS")) return "Hesaplar Arası Transfer";
            if (t.TrimStart().StartsWith("BANK CHARGES") || t.TrimStart().StartsWith("BANK FEES")) return "Banka Masrafı";   // yalnız masraf satırı (Uras TR transfer masrafı dahil); mal ödemesi satırındaki "+ BANK CHARGES" eki mal ödemesi sayılır
            if (V("INSTALMENT URAS", "INSTALLMENT URAS")) return "Uras Türkiye'den Gelen";
            if (V("IHR", "URAS TR", "URAS TURKEY", "URAS KİMYA", "URAS KIMYA", "YOUR INVOICE") && !V("SAMPLES")) return "Uras Türkiye Ödemesi";
            if (V("SALARY", "SALARIES", "SUBSIDY", "ALLOWANCE")) return V("RETENTION") ? "Vergi ve SGK" : "Maaş ve İkramiye";
            if (V("SOCIAL SECURITY", "SOCIAL INSURANCE", "RETENTION", "FISCAL", "COMPENSATION FUND", "IGAMAOT", "BUSINESS INFORMATION", "NOTARY") || W("VAT") || W("TAX") || W("FINE")) return "Vergi ve SGK";
            if (W("RENT") && !V("PRINTER")) return "Kira";
            if (V("TRANSPORT", "CUSTOMS", "DHL", "CONTAINER", "PALLET", "LOGISTICS", "FREIGHT")) return "Nakliye ve Gümrük";
            if (V("TOLL", "DIESEL", "FUEL", "ADBLUE", "CAR", "PLATE", "TIRES", "PEUGEOT", "CITROEN", "MECHANIC")) return "Araç ve Yakıt";
            if (V("ACCOUNTAN", "CONSULT", "LAWYER", "TRANSLAT", "REACH", "INNOVATION", "INOVATION")) return "Muhasebe ve Danışmanlık";
            if (V("BANK CHARGES", "BANK FEES")) return "Banka Masrafı";
            if (V("EDP", "ENERGY", "VODAFONE", "WATER", "TELEPHONE", "INTERNET", "SECURITAS", "CLEANING", "GRENKE", "XDOC", "PRINTER", "INSURANCE", "GENERAL EXPENSES", "NOSSOPODIUM", "SOFTWARE", "VNI")) return "Genel Giderler";
            if (V("WITHDRAW", "RESTAURANT", "HOTEL", "FLIGHT", "LUNCH")) return "Seyahat ve Temsil";
            return giris ? "Diğer Giriş" : "Diğer Gider";
        }
    }
}
