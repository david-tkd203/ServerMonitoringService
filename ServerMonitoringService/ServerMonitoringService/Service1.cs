using System;
using System.ServiceProcess;
using System.Threading;
using System.Configuration;
using System.Net.Http;
using System.Net.Mail;

namespace ServerMonitoringService
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer = new Timer(CheckServerStatus, null, 0, Timeout.Infinite);
        }

        protected override void OnStop()
        {
            timer?.Change(Timeout.Infinite, Timeout.Infinite);
            timer?.Dispose();
        }

        private void CheckServerStatus(object state)
        {
            try
            {
                bool isServerDown = CheckServerDown();

                if (isServerDown)
                {
                    SendEmailNotification();
                }
            }
            finally
            {
                timer?.Change(60000, Timeout.Infinite);
            }
        }

        private bool CheckServerDown()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(ConfigurationManager.AppSettings["ApiUrl"]).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (HttpRequestException ex)
            {
                // Manejar errores relacionados con la solicitud HTTP
                Console.WriteLine($"Error al realizar la solicitud HTTP: {ex.Message}");
                return true; // Puedes ajustar el valor de retorno según tus necesidades
            }
            catch (Exception ex)
            {
                // Manejar otras excepciones
                Console.WriteLine($"Error al verificar el estado del servidor: {ex.Message}");
                return true; // Puedes ajustar el valor de retorno según tus necesidades
            }
        }


        private void SendEmailNotification()
        {
            try
            {
                using (SmtpClient smtpClient = new SmtpClient(ConfigurationManager.AppSettings["MailServer"])
                {
                    Port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
                    Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["MailUsername"], ConfigurationManager.AppSettings["MailPassword"]),
                    EnableSsl = true,
                })
                {
                    using (MailMessage mailMessage = new MailMessage(ConfigurationManager.AppSettings["MailUsername"], ConfigurationManager.AppSettings["ReceiverEmail"])
                    {
                        Subject = "¡ALERTA! El servidor está caído",
                        Body = "El servidor está inaccesible. Por favor, toma medidas.",
                    })
                    {
                        smtpClient.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar errores al enviar el correo
                Console.WriteLine($"Error al enviar el correo: {ex.Message}");
                throw; // Relanzar la excepción para el registro en los registros del sistema
            }
        }

    }
}


