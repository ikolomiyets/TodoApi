namespace TodoApi.RabbitMq
{
    public class RabbitOptions
    {
        public string UserName { get; set; }  
  
        public string Password { get; set; }

        public string HostName { get; set; } = "localhost";
  
        public int Port { get; set; } = 5672;  
  
        public string VHost { get; set; } = "/";      
  
        public string Prefix { get; set; } = "Todo";      
  
        public int Ttl { get; set; } = 60;      
  
        public int RetrieveTimeout { get; set; } = 10;      
    }
}