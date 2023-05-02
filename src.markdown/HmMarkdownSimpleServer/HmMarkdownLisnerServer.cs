using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace HmMarkdownSimpleServer;

// Filename:  HttpServer.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)

class HttpMarkdownListener
{
    public static HttpListener listener;
    public static int pageViews = 0;
    public static int requestCount = 0;
    public static string pageData = "";

    static bool runServer = false;
    public static void HandleIncomingConnections()
    {
        runServer = true;

        // リスナー設定
        listener.Prefixes.Clear();
        listener.Prefixes.Add(@"http://localhost:50333/");

        // リスナー開始
        listener.Start();

        while (runServer)
        {
            Trace.WriteLine("リスニング");
            // リクエスト取得
            HttpListenerContext context = listener.GetContext();
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            HttpListenerRequest request = context.Request;

            // レスポンス取得
            HttpListenerResponse response = context.Response;

            // HTMLを表示する
            if (request != null)
            {
                byte[] text = Encoding.UTF8.GetBytes("れすぽんす");
                response.ContentType = "text/json; charset=utf-8";
                response.ContentEncoding = Encoding.UTF8;
                response.OutputStream.Write(text, 0, text.Length);
            }
            else
            {
                response.StatusCode = 404;
            }
            response.Close();
        }
    }


    public static void Start()
    {
        Trace.WriteLine("スタート");
        // Create a Http server and start listening for incoming connections
        listener = new HttpListener();

        HandleIncomingConnections();
    }

    public static void Close()
    {
        runServer = false;
        // Close the listener
        listener.Close();
    }
}

