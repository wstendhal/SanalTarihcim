# Sanal Tarihçim

## Gmail ile doğrulama e-postası

Uygulama Gmail SMTP sunucusunu (`smtp.gmail.com`, port `587`, SSL/TLS) kullanacak şekilde ayarlıdır. Gönderici hesabında 2 Adımlı Doğrulama'yı açıp bir Google Uygulama Şifresi oluşturun. Normal Gmail hesap şifrenizi kullanmayın.

### Yerel geliştirme

Proje klasöründe gizli ayarları User Secrets'a kaydedin:

```powershell
dotnet user-secrets set "Smtp:From" "gonderici@gmail.com" --project .\SanalTarihcim.csproj
dotnet user-secrets set "Smtp:Username" "gonderici@gmail.com" --project .\SanalTarihcim.csproj
dotnet user-secrets set "Smtp:Password" "google-uygulama-sifresi" --project .\SanalTarihcim.csproj
```

Ardından projeyi çalıştırıp kayıt ekranından kendi e-posta adresinizle deneyin. Kod 10 dakika geçerlidir. E-posta ayarları eksik veya yanlışsa uygulama kodu ekranda göstermez; gönderim başarısızlığını bildirir.

### Yayın ortamı

GitHub deposuna SMTP parolası eklemeyin. Uygulamayı barındıran sunucunun gizli ortam değişkenlerinde şu değerleri tanımlayın:

| Ortam değişkeni | Değer |
| --- | --- |
| `Smtp__Host` | `smtp.gmail.com` |
| `Smtp__Port` | `587` |
| `Smtp__EnableSsl` | `true` |
| `Smtp__From` | Gönderici Gmail adresi |
| `Smtp__Username` | Gönderici Gmail adresi |
| `Smtp__Password` | Google Uygulama Şifresi |

GitHub deposuna push etmek, ASP.NET Core uygulamasını kendiliğinden yayınlamaz. Gerçek e-posta gönderimi için uygulamanın çalıştığı bir hosting ortamı ve yukarıdaki gizli ayarlar gereklidir.
