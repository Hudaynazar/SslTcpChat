# SslTcpChat

Bu proje, C# ve .NET kullanılarak geliştirilmiş bir SSL/TLS şifrelemeli TCP sohbet uygulamasıdır.
Burada socket öğrendiğim için kullanıcı sadece server ile konuşabiliyor. Server çok kullanıcı destekli konuşabiliyor. Bir sonraki sürümünde kullanıcılar kendi aralarında konuşması için güncelleyecğim. Fakat en geneli bu koddur.

## Projeler

- `Server`: SSL sertifikası(şifreleme) ile gelen istemcileri dinleyen sunucu.
- `Client`: Kullanıcıdan mesaj alıp sunucuya gönderen istemci.

## Kullanım

1. `Server` projesini çalıştırın.
2. Ardından `Client` projesini çalıştırarak bağlantı kurun.

## Sertifika

Sunucu tarafında `cert.pfx` dosyası gerekmektedir.

> `cert.pfx` dosyasını "...Server1/Server1" klasörüne koymayı unutmayın.
