namespace _2Korriku.Services;

public static class LoginLanguage
{
    public const string Cookie = "UPNEXT.LoginLanguage";
    public static readonly string[] Codes = ["sq", "en", "sr", "tr"];
    public static string Normalize(string? code) => Codes.Contains(code) ? code! : "sq";
    private static readonly Dictionary<string,string[]> Texts = new()
    {
        ["Install"] = ["Instalo aplikacionin", "Install app", "Instaliraj aplikaciju", "Uygulamayı yükle"],
        ["InstallHelp"] = ["Për instalim në iPhone/iPad: hapeni në Safari, prekni Share, pastaj Add to Home Screen.", "To install on iPhone/iPad: open in Safari, tap Share, then Add to Home Screen.", "Za instalaciju na iPhone/iPad uređaju: otvorite u Safariju, dodirnite Share, zatim Add to Home Screen.", "iPhone/iPad'e yüklemek için Safari'de açın, Share ve ardından Add to Home Screen seçeneğine dokunun."],
        ["Dismiss"] = ["Mbyll njoftimin e instalimit", "Dismiss installation notice", "Zatvori obaveštenje o instalaciji", "Yükleme bildirimini kapat"],
        ["Title"] = ["Hyr në llogari", "Sign in", "Prijavite se", "Giriş yap"],
        ["Identity"] = ["Identifikimi", "Sign in", "Prijava", "Oturum açma"],
        ["Tagline"] = ["Klubi yt. Një hap përpara.", "Your club. One step ahead.", "Vaš klub. Korak ispred.", "Kulübünüz. Bir adım önde."],
        ["Platform"] = ["PLATFORMA UPNEXT", "UPNEXT PLATFORM", "UPNEXT PLATFORMA", "UPNEXT PLATFORMU"],
        ["Continue"] = ["Vazhdo me llogarinë tënde.", "Continue with your account.", "Nastavite sa svojim nalogom.", "Hesabınızla devam edin."],
        ["Email"] = ["Adresa e emailit", "Email address", "Imejl adresa", "E-posta adresi"],
        ["EmailPlaceholder"] = ["emri@shembull.com", "name@example.com", "ime@primer.com", "ad@ornek.com"],
        ["Password"] = ["Fjalëkalimi", "Password", "Lozinka", "Şifre"],
        ["PasswordPlaceholder"] = ["Shkruani fjalëkalimin", "Enter your password", "Unesite lozinku", "Şifrenizi girin"],
        ["Show"] = ["Shfaq fjalëkalimin", "Show password", "Prikaži lozinku", "Şifreyi göster"],
        ["Hide"] = ["Fshih fjalëkalimin", "Hide password", "Sakrij lozinku", "Şifreyi gizle"],
        ["Remember"] = ["Më mbaj të kyçur", "Keep me signed in", "Ostani prijavljen", "Oturumumu açık tut"],
        ["Credit"] = ["Programuar nga", "Developed by", "Razvio", "Geliştiren"],
        ["Language"] = ["Gjuha", "Language", "Jezik", "Dil"],
        ["EmailRequired"] = ["Shkruani adresën e emailit.", "Enter your email address.", "Unesite imejl adresu.", "E-posta adresinizi girin."],
        ["EmailInvalid"] = ["Shkruani një adresë emaili të vlefshme.", "Enter a valid email address.", "Unesite važeću imejl adresu.", "Geçerli bir e-posta adresi girin."],
        ["PasswordRequired"] = ["Shkruani fjalëkalimin.", "Enter your password.", "Unesite lozinku.", "Şifrenizi girin."],
        ["Locked"] = ["Llogaria është bllokuar përkohësisht. Provoni përsëri më vonë.", "Your account is temporarily locked. Try again later.", "Nalog je privremeno zaključan. Pokušajte ponovo kasnije.", "Hesabınız geçici olarak kilitlendi. Daha sonra tekrar deneyin."],
        ["Invalid"] = ["Emaili ose fjalëkalimi nuk është i saktë.", "The email or password is incorrect.", "Imejl adresa ili lozinka nisu tačni.", "E-posta adresi veya şifre yanlış."]
    };
    public static string Text(string language, string key) => Texts[key][Array.IndexOf(Codes, Normalize(language))];
}
