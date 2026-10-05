# Sanal Tarihçim

## Brevo ile doğrulama e-postası

Uygulama, kullanıcı doğrulama e-postaları için Brevo SMTP kullanacak şekilde ayarlıdır (`smtp-relay.brevo.com`, port `587`, STARTTLS). Gmail hesabında 2 Adımlı Doğrulama açmanız gerekmez.

1. [Brevo](https://www.brevo.com/) hesabı oluşturup e-posta gönderimini etkinleştirin.
2. Brevo'da gönderici olarak kullanacağınız e-posta adresini ekleyip doğrulayın. Kendi alan adınızla gönderim yapmak için Brevo'nun istediği alan adı doğrulama/DNS kayıtlarını da tamamlayın.
3. Brevo SMTP ayarlarından **SMTP login** ve **SMTP key** değerlerini alın. SMTP key yerine API key veya e-posta hesabınızın normal parolasını kullanmayın.

### Yerel geliştirme

Proje klasöründe gizli ayarları User Secrets'a kaydedin. `From` doğruladığınız gönderici adresi; `Username` ve `Password` ise Brevo'nun verdiği SMTP login ve SMTP key olmalıdır:

```powershell
dotnet user-secrets set "Smtp:From" "Brevo'da doğruladığınız gönderici adresi" --project .\SanalTarihcim.csproj
dotnet user-secrets set "Smtp:Username" "Brevo SMTP login" --project .\SanalTarihcim.csproj
$secure = Read-Host "Brevo SMTP key" -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    dotnet user-secrets set "Smtp:Password" $password --project .\SanalTarihcim.csproj
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    Remove-Variable password, secure, pointer -ErrorAction SilentlyContinue
}
```

Ardından projeyi çalıştırıp kayıt ekranından kendi e-posta adresinizle deneyin. Kod 10 dakika geçerlidir. E-posta ayarları eksik veya yanlışsa uygulama kodu ekranda göstermez; gönderim başarısızlığını bildirir.

### Yayın ortamı

GitHub deposuna SMTP parolası eklemeyin. Uygulamayı barındıran sunucunun gizli ortam değişkenlerinde şu değerleri tanımlayın:

| Ortam değişkeni | Değer |
| --- | --- |
| `Smtp__Host` | `smtp-relay.brevo.com` |
| `Smtp__Port` | `587` |
| `Smtp__EnableSsl` | `true` |
| `Smtp__From` | Brevo'da doğruladığınız gönderici adresi |
| `Smtp__Username` | Brevo SMTP login |
| `Smtp__Password` | Brevo SMTP key |

SMTP key'i GitHub'a veya kaynak koduna eklemeyin. GitHub deposuna push etmek, ASP.NET Core uygulamasını kendiliğinden yayınlamaz. Gerçek e-posta gönderimi için uygulamanın çalıştığı bir hosting ortamı ve yukarıdaki gizli ayarlar gereklidir.
