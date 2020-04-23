using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;

namespace FolderSyncTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        string[] FilesPath;
        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {                
                textBox1.Text = folderBrowserDialog1.SelectedPath;
                FileInfo[] files = new DirectoryInfo(folderBrowserDialog1.SelectedPath).GetFiles();
                FilesPath = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    FilesPath[i] = files[i].FullName;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog()==DialogResult.OK)
            {
                FilesPath = openFileDialog1.FileNames;
                textBox1.Text = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);
                textBox4.Text="";
                for (int i = 0; i < openFileDialog1.SafeFileNames.Length; i++)
                {
                    textBox4.Text = textBox4.Text + "\"" + openFileDialog1.SafeFileNames[i] + "\"";
                }
                
            }
        }

        private static void EndGetStreamCallback(IAsyncResult ar)
        {
            FtpState state = (FtpState)ar.AsyncState;

            Stream requestStream = null;
            // End the asynchronous call to get the request stream.
            try
            {
                requestStream = state.Request.EndGetRequestStream(ar);
                // Copy the file contents to the request stream.
                const int bufferLength = 2048;
                byte[] buffer = new byte[bufferLength];
                int count = 0;
                int readBytes = 0;
                FileStream stream = File.OpenRead(state.FileName);
                do
                {
                    readBytes = stream.Read(buffer, 0, bufferLength);
                    requestStream.Write(buffer, 0, readBytes);
                    count += readBytes;
                }
                while (readBytes != 0);
                Console.WriteLine("Writing {0} bytes to the stream.", count);
                // IMPORTANT: Close the request stream before sending the request.
                requestStream.Close();
                // Asynchronously get the response to the upload request.
                state.Request.BeginGetResponse(
                    new AsyncCallback(EndGetResponseCallback),
                    state
                );
            }
            // Return exceptions to the main application thread.
            catch (Exception e)
            {
                Console.WriteLine("Could not get the request stream.");
                state.OperationException = e;
                state.OperationComplete.Set();
                return;
            }
        }

        // The EndGetResponseCallback method  
        // completes a call to BeginGetResponse.
        private static void EndGetResponseCallback(IAsyncResult ar)
        {
            FtpState state = (FtpState)ar.AsyncState;
            FtpWebResponse response = null;
            try
            {
                response = (FtpWebResponse)state.Request.EndGetResponse(ar);
                response.Close();
                state.StatusDescription = response.StatusDescription;
                // Signal the main application thread that 
                // the operation is complete.
                state.OperationComplete.Set();
            }
            // Return exceptions to the main application thread.
            catch (Exception e)
            {
                Console.WriteLine("Error getting response.");
                state.OperationException = e;
                state.OperationComplete.Set();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Create a Uri instance with the specified URI string.
            // If the URI is not correctly formed, the Uri constructor
            // will throw an exception.
            ManualResetEvent waitObject;
            for (int i = 0; i < FilesPath.Length; i++)
            {
                Uri target = new Uri(@"ftp://" + textBox2.Text + textBox3.Text + System.IO.Path.GetFileName(FilesPath[i]));
                string fileName = FilesPath[i];
                FtpState state = new FtpState();
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(target);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // This example uses anonymous logon.
                // The request is anonymous by default; the credential does not have to be specified. 
                // The example specifies the credential only to
                // control how actions are logged on the server.

                request.Credentials = new NetworkCredential("root", "Festo");

                // Store the request in the object that we pass into the
                // asynchronous operations.
                state.Request = request;
                state.FileName = fileName;

                // Get the event to wait on.
                waitObject = state.OperationComplete;

                // Asynchronously get the stream for the file contents.
                request.BeginGetRequestStream(
                    new AsyncCallback(EndGetStreamCallback),
                    state
                );

                // Block the current thread until all operations are complete.
                waitObject.WaitOne();

                // The operations either completed or threw an exception.
                if (state.OperationException != null)
                {
                    //throw state.OperationException;
                }
                else
                {
                    MessageBox.Show("The operation completed - {0}", state.StatusDescription);
                }
            }
            
        }
    }

    public class FtpState
    {
        private ManualResetEvent wait;
        private FtpWebRequest request;
        private string fileName;
        private Exception operationException = null;
        string status;

        public FtpState()
        {
            wait = new ManualResetEvent(false);
        }

        public ManualResetEvent OperationComplete
        {
            get { return wait; }
        }

        public FtpWebRequest Request
        {
            get { return request; }
            set { request = value; }
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }
        public Exception OperationException
        {
            get { return operationException; }
            set { operationException = value; }
        }
        public string StatusDescription
        {
            get { return status; }
            set { status = value; }
        }
    }
}
