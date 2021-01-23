/*
 * This file is part of RemotePrinter.
 * Copyright (C) 2018-2019 Juan José Prieto Dzul <juanjoseprieto88@gmail.com>
 * 
 * RemotePrinter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * RemotePrinter is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with RemotePrinter.  If not, see<https://www.gnu.org/licenses/>.
 */

using RestSharp;
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace RemotePrinter
{
    public class PrintServer
    {
        private HttpListener httpListener;
        private static AutoResetEvent listenForNextRequest = new AutoResetEvent(false);

        public string Prefix { get; set; }
        public bool IsRunning { get; private set; }

        //Delegate of type Action<string>
        private static Action<string> StatusUpdater;

        public PrintServer(Action<string> UpdateStatusDelegate)
        {
            httpListener = new HttpListener();
            StatusUpdater = UpdateStatusDelegate;            
        }

        public void Start()
        {
            // Verify HttpListener is supported
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

            // Add prefix
            if (String.IsNullOrEmpty(Prefix))
                throw new InvalidOperationException("Specify prefix");
            httpListener.Prefixes.Clear();
            httpListener.Prefixes.Add(Prefix);

            // Start server
            httpListener.Start();
            ThreadPool.SetMaxThreads(10, 10);
            ThreadPool.QueueUserWorkItem(ListenRequest);
            //Thread.Sleep(3000);
            //StatusUpdater("Server runing fine");
        }

        internal void Stop()
        {
            httpListener.Stop();
            IsRunning = false;
        }

        private void ListenRequest(object state)
        {
            // Loop here to begin processing of new requests.
            while (httpListener.IsListening)
            {
                StatusUpdater("Servidor de impresion OK");

                httpListener.BeginGetContext(new AsyncCallback(ListenerCallback), httpListener);
                listenForNextRequest.WaitOne();

                IsRunning = true;
            }
        }

        private static void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);

            if (context.Request.HttpMethod.Equals("GET"))
            {
                GetRequestProcess(context);
            }

            // Process next request
            result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
        }

        private static void GetRequestProcess(HttpListenerContext context)
        {
            var query = context.Request.QueryString["documento"];
            var url = Properties.Settings.Default.server;
            var token = Properties.Settings.Default.apikey;

            string browserMessage = "Servidor corriendo";

            if (string.IsNullOrEmpty(query))
            {
                writeBrowserMessage(context, browserMessage);
                context.Response.Close();
                return;
            }

            RestClient cliente = new RestClient(url);
            RestRequest request = new RestRequest("api/3/ticketes/{id}", Method.GET);

            request.AddHeader("Token", token);
            request.AddUrlSegment("id", query);
            request.Timeout = 2000;

            IRestResponse<Ticket> response = cliente.Execute<Ticket>(request);

            if (response.ErrorException != null)
            {
                Console.WriteLine("Error {0}: {1}", response.StatusCode, response.ErrorMessage);
                context.Response.StatusCode = 408;
                context.Response.StatusDescription = "Request Timeout";

                browserMessage = "Error al imprimir";
            }

            if (response.IsSuccessful)
            {            
                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                var data = response.Data;
                PrintManager.Print(data.text, data.cortarpapel, data.abrircajon);

                browserMessage = "Impresion correcta";                
            }            

            // Response
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET");

            writeBrowserMessage(context, browserMessage);
            context.Response.Close();
        }

        private static void writeBrowserMessage(HttpListenerContext context, string message)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(message);
            context.Response.ContentLength64 = encoded.Length;
            context.Response.OutputStream.Write(encoded, 0, encoded.Length);
            context.Response.OutputStream.Close();
        }
    }
}