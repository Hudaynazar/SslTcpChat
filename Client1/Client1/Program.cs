using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Client1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TcpClient tcpClient = new TcpClient("localhost", XXXX); //burada portunuz

                SslStream sslStream = new SslStream(
                    tcpClient.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    null);

                sslStream.AuthenticateAsClient("localhost");

                Console.WriteLine("Sunucuya güvenli bağlantı sağlandı.");

                Task.Run(() => SendMessage(sslStream));

                while (true)
                {
                    if (!IsConnected(tcpClient))
                    {
                        Console.WriteLine("Bağlantı koptu!");
                        break;
                    }

                    try
                    {
                        string gelen = ReceiveMessage(sslStream);
                        if (gelen == null)
                            break;

                        Console.WriteLine("Server: " + gelen);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("İletişim hatası: " + ex.Message);
                    }
                }

                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sunucuya bağlanılamadı: " + ex.Message);
            }
        }

        // Sertifika doğrulaması (şu anlık her şeyi kabul ediyoruz)
        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static string ReceiveMessage(SslStream stream)
        {
            try
            {
                byte[] uzunlukBytes = new byte[4];
                stream.Read(uzunlukBytes, 0, 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(uzunlukBytes);

                int uzunluk = BitConverter.ToInt32(uzunlukBytes, 0);
                byte[] mesajBytes = new byte[uzunluk];
                int toplamOkunan = 0;

                while (toplamOkunan < uzunluk)
                {
                    int okunan = stream.Read(mesajBytes, toplamOkunan, uzunluk - toplamOkunan);
                    if (okunan == 0) break;
                    toplamOkunan += okunan;
                }

                return Encoding.UTF8.GetString(mesajBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mesaj alma hatası: " + ex.Message);
                return null;
            }
        }

        static void SendMessage(SslStream stream)
        {
            while (true)
            {
                try
                {
                    Console.Write("> ");
                    string mesaj = Console.ReadLine();

                    byte[] mesajBytes = Encoding.UTF8.GetBytes(mesaj);
                    int mesajUzunlugu = mesajBytes.Length;
                    byte[] uzunlukBytes = BitConverter.GetBytes(mesajUzunlugu);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(uzunlukBytes);

                    byte[] gonderilecek = new byte[4 + mesajUzunlugu];
                    Array.Copy(uzunlukBytes, 0, gonderilecek, 0, 4);
                    Array.Copy(mesajBytes, 0, gonderilecek, 4, mesajUzunlugu);

                    stream.Write(gonderilecek);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Gönderme hatası: " + ex.Message);
                    break;
                }
            }
        }

        static bool IsConnected(TcpClient client)
        {
            try
            {
                if (client == null || !client.Connected)
                    return false;

                if (client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Available == 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
