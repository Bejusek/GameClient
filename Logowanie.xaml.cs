﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Commands;

namespace RPGClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //ustawienie obiektu klienta TCP/IP
        private TcpClient user;
        
        //ustawienie kodera/dekodera
        private UTF8Encoding code;

        //ustawienie hosta serwera
        private string host;

        //ustawienie portu, na którym nasłuchuje serwer
        private int port;

        //wątek sprawdzania czy udało się zalogować
        private Thread loginTh;

        //delegat funkcji przyjmującej jako argument bool
        private delegate void SetBool(bool logic);

        public MainWindow()
        {
            InitializeComponent();

            host = Properties.Settings.Default.Host;
            port = Properties.Settings.Default.Port;
        }
        
        //funkcja konwerująca ciąg znaków na kod MD5
        public string GetMD5Hash(string input)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            string password = s.ToString();
            return password;
        }

        //zamiana stringa cmd na akcję i ciąg argumentów
        private string[] cmdToArgs(string command)
        {
            string[] args = command.Split(';');
            return args;
        }
        
        //logowanie do gry
        private void singin_Click(object sender, RoutedEventArgs e)
        {
            //inicjalizacja gniazda
            try
            {
                user = new TcpClient(host, port);
                
                //ustawienie czasu oczekiwania na odpowiedź serwera
                user.ReceiveTimeout = 5;
            }
            catch
            {
                MessageBox.Show("Czas oczekiwania na połączenie minął. Nie można było połączyć się z serwerem. Sprawdź połączenia z Internetami.", "Błąd połączenia!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //inicjalizacja kodera/dekodera
            code = new UTF8Encoding();

            //inicjalizacja bufora przechowującego dane do wysłania
            byte[] buf = new byte[4096];

            Command cmd = new Command();
            cmd.Request(ClientCmd.LOGIN);
            cmd.Add(login.Text);
            cmd.Add(GetMD5Hash(password.Password));
            cmd.Apply();

            //wysłanie do serwera informacji o chęci zalogowania + dane logowania
            user.Client.Send(cmd.Byte);

            try
            {
                string response;
                //dopóki serwer nie odpowiada
                while (true)
                {
                    if (user.Available > 0)
                    {
                        //zmienna przechowująca odpowiedź serwera
                        response = code.GetString(buf, 0, user.Client.Receive(buf));
                        break;
                    }
                    Thread.Sleep(1);
                }

                //jeżeli serwer odpowie to zapisz odpowiedź do response

                string[] args = cmdToArgs(response);
                if (Convert.ToUInt64(args[1]) == 0)
                {
                    MessageBox.Show("[Klient] Nie udało się zalogować.");
                    user.Close();
                    singin.IsEnabled = true;
                    return;
                }
                else
                {
                    new Interface(Convert.ToUInt64(args[1]), user).Show();
                    this.Close();
                }
            }
            catch
            {
                return;
            }
        }
    }
}
