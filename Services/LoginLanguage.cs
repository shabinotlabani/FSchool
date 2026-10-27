namespace _2Korriku.Services;

public static class LoginLanguage
{
    public const string Cookie = "UPNEXT.LoginLanguage";
    public static readonly string[] Codes = ["sq", "en"];
    public static string Normalize(string? code) => Codes.Contains(code) ? code! : "sq";
    private static readonly Dictionary<string,string[]> Texts = new()
    {
        ["HeroLead"] = ["Klubi yt.", "Your club."],
        ["HeroAccent"] = ["Një hap përpara.", "One step ahead."],
        ["HeroDescription"] = ["Më shumë kohë për sportin. Gjithçka tjetër, në një vend.", "More time for sport. Everything else, in one place."],
        ["Sports"] = ["Futboll · Basketboll · Hendboll · Dhe më shumë", "Football · Basketball · Handball · And more"],
        ["Players"] = ["Lojtarët", "Players"],
        ["Training"] = ["Trajnimet", "Training"],
        ["Finances"] = ["Financat", "Finances"],
        ["Statistics"] = ["Statistikat", "Statistics"],
        ["Workspace"] = ["HAPËSIRA E KLUBIT", "CLUB WORKSPACE"],
        ["Loading"] = ["Duke hyrë…", "Signing in…"],
        ["Install"] = ["Instalo aplikacionin", "Install app"],
        ["InstallHelp"] = ["Për instalim në iPhone/iPad: hapeni në Safari, prekni Share, pastaj Add to Home Screen.", "To install on iPhone/iPad: open in Safari, tap Share, then Add to Home Screen."],
        ["Dismiss"] = ["Mbyll njoftimin e instalimit", "Dismiss installation notice"],
        ["Title"] = ["Hyr në llogari", "Sign in"],
        ["Identity"] = ["Identifikimi", "Sign in"],
        ["Tagline"] = ["Klubi yt. Një hap përpara.", "Your club. One step ahead."],
        ["Platform"] = ["PLATFORMA UPNEXT", "UPNEXT PLATFORM"],
        ["Continue"] = ["Vazhdo me llogarinë tënde.", "Continue with your account."],
        ["Email"] = ["Adresa e emailit", "Email address"],
        ["EmailPlaceholder"] = ["emri@shembull.com", "name@example.com"],
        ["Password"] = ["Fjalëkalimi", "Password"],
        ["PasswordPlaceholder"] = ["Shkruani fjalëkalimin", "Enter your password"],
        ["Show"] = ["Shfaq fjalëkalimin", "Show password"],
        ["Hide"] = ["Fshih fjalëkalimin", "Hide password"],
        ["Remember"] = ["Më mbaj të kyçur", "Keep me signed in"],
        ["Credit"] = ["Programuar nga", "Developed by"],
        ["Language"] = ["Gjuha", "Language"],
        ["EmailRequired"] = ["Shkruani adresën e emailit.", "Enter your email address."],
        ["EmailInvalid"] = ["Shkruani një adresë emaili të vlefshme.", "Enter a valid email address."],
        ["PasswordRequired"] = ["Shkruani fjalëkalimin.", "Enter your password."],
        ["Locked"] = ["Llogaria është bllokuar përkohësisht. Provoni përsëri më vonë.", "Your account is temporarily locked. Try again later."],
        ["Invalid"] = ["Emaili ose fjalëkalimi nuk është i saktë.", "The email or password is incorrect."]
    };
    public static string Text(string language, string key) => Texts[key][Array.IndexOf(Codes, Normalize(language))];
}
