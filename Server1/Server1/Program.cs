using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Server1
{
    internal class Program
    {
        static Dictionary<string, SslStream> clientStreams = new Dictionary<string, SslStream>();
        static object clientLock = new object();

        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("XXX.XXX.XXX.XXX"); //BURAYA SİZİN YEREL İP'niz GELİR
            int port = XXXX; //Buraya portunuz

            try
            {
                TcpListener server = new TcpListener(ip, port);
                server.Start();
                Console.WriteLine("Sunucu başlatıldı. Bağlantı bekleniyor...");

                int clientCount = 0;

                Task.Run(() => ServerConsoleInput());

                X509Certificate2 certificate = new X509Certificate2("cert.pfx", "SZİN ŞİFRENİZ"); //BURAYA SİZİN ŞİFRENİZ GELMEDLİDİR

                while (true)
                {
                    TcpClient client = null;
                    try
                    {
                        client = server.AcceptTcpClient();
                        clientCount++;
                        string clientName = "Client" + clientCount;

                        SslStream sslStream = new SslStream(client.GetStream(), false);
                        sslStream.AuthenticateAsServer(certificate, false, false);

                        lock (clientLock)
                        {
                            clientStreams.Add(clientName, sslStream);
                        }

                        Console.WriteLine($"{clientName} bağlandı.");

                        Task.Run(() => HandleClient(client, sslStream, clientName));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("İstemci Bağlanamadı: " + ex.ToString());
                        client?.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sunucu Başlatılamadı: " + ex.ToString());
            }
        }

        static void HandleClient(TcpClient client, SslStream sslStream, string clientName)
        {
            try
            {
                while (true)
                {
                    if (!IsConnected(client))
                    {
                        Console.WriteLine("Bağlantı Koptu");
                        break;
                    }

                    string gelen = ReceiveMessage(sslStream);
                    if (gelen == null)
                        break;

                    Console.WriteLine($"{clientName} dedi ki: {gelen}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Veri Alınamadı: " + ex.Message);
            }
            finally
            {
                lock (clientLock)
                {
                    clientStreams.Remove(clientName);
                }
                client.Close();
                Console.WriteLine($"{clientName} bağlantısı kapandı.");
            }
        }

        static string ReceiveMessage(SslStream sslStream)
        {
            try
            {
                byte[] uzunlukBytes = new byte[4];
                sslStream.Read(uzunlukBytes, 0, 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(uzunlukBytes);

                int uzunluk = BitConverter.ToInt32(uzunlukBytes, 0);

                byte[] messageByte = new byte[uzunluk];
                int toplamOkunan = 0;

                while (toplamOkunan < uzunluk)
                {
                    int okunan = sslStream.Read(messageByte, toplamOkunan, uzunluk - toplamOkunan);
                    if (okunan == 0)
                        break;
                    toplamOkunan += okunan;
                }
                return Encoding.UTF8.GetString(messageByte);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mesaj alma hatası: " + ex.ToString());
                return null;
            }
        }

        static void SendMessage(SslStream sslStream, string message)
        {
            try
            {
                byte[] mesajBytes = Encoding.UTF8.GetBytes(message);
                int mesajUzunluğu = mesajBytes.Length;

                byte[] uzunlukBytes = BitConverter.GetBytes(mesajUzunluğu);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(uzunlukBytes);

                byte[] gonderilecek = new byte[uzunlukBytes.Length + mesajUzunluğu];
                Array.Copy(uzunlukBytes, 0, gonderilecek, 0, uzunlukBytes.Length);
                Array.Copy(mesajBytes, 0, gonderilecek, uzunlukBytes.Length, mesajUzunluğu);

                sslStream.Write(gonderilecek, 0, gonderilecek.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mesaj gönderme hatası: " + ex);
            }
        }

        static void ServerConsoleInput()
        {
            while (true)
            {
                Console.WriteLine("Kime mesaj göndermek istiyorsun? (Mevcutlar: " + string.Join(", ", clientStreams.Keys) + ")");
                Console.Write("İsim: ");
                string hedef = Console.ReadLine();

                lock (clientLock)
                {
                    if (clientStreams.ContainsKey(hedef))
                    {
                        Console.Write("Mesaj: ");
                        string mesaj = Console.ReadLine();
                        SendMessage(clientStreams[hedef], mesaj);
                    }
                    else
                    {
                        Console.WriteLine("Bu isimde bir istemci yok.");
                    }
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
