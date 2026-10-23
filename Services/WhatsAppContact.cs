using System.Text.RegularExpressions;

namespace _2Korriku.Services;

public record WhatsAppContact(string? Phone,string? Name)
{
    public static string? Link(string? phone)
    {
        if(string.IsNullOrWhiteSpace(phone))return null;
        var value=Regex.Replace(phone.Trim(),@"[\s().-]","");
        if(value.StartsWith('+'))value=value[1..];
        else if(value.StartsWith("00"))value=value[2..];
        else if(Regex.IsMatch(value,@"^04\d{7}$"))value="383"+value[1..];
        else if(!value.StartsWith("383"))return null;
        return Regex.IsMatch(value,@"^[1-9][0-9]{7,14}$")?"https://wa.me/"+value:null;
    }
}
