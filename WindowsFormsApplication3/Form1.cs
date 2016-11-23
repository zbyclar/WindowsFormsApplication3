using System;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace WindowsFormsApplication3
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Form1 : Form
    {
       
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            webBrowser1.ObjectForScripting = this;
            TreeNode t1 = treeView1.Nodes.Add("GPS 联系人");
            TreeNode t2 = new TreeNode("116.307414,39.910573");
            GlobalVariable.gpsAllLocation += t2.ToString().Substring(10) + "|";
            TreeNode t3 = new TreeNode("116.292466,39.98403");
            GlobalVariable.gpsAllLocation += t3.ToString().Substring(10) + "|";
            TreeNode t4 = new TreeNode("116.433896,39.975184");
            GlobalVariable.gpsAllLocation += t4.ToString().Substring(10) + "|";
            t1.Nodes.Add(t2);
            t1.Nodes.Add(t3);
            t1.Nodes.Add(t4);
            Console.WriteLine(GlobalVariable.gpsAllLocation);
            
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            webBrowser1.Navigate(new Uri("c:/users/zbyclar/documents/visual studio 2015/Projects/WindowsFormsApplication3/WindowsFormsApplication3/BaiduMap.html"));
            
            
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.Document != null)
            {
                int length = GlobalVariable.gpsAllLocation.Split('|').Length;
               
                String[] gpsTotalLocation = new String[2 * length];     
                for(int i = 0; i < length; i++)
                {
                    if (GlobalVariable.gpsAllLocation.Split('|')[i] != "")
                    {
                        Console.WriteLine(GlobalVariable.gpsAllLocation.Split('|')[i]);
                        Console.WriteLine(GlobalVariable.gpsAllLocation.Split('|')[i].Split(',')[0]);
                        Console.WriteLine(GlobalVariable.gpsAllLocation.Split('|')[i].Split(',')[1]);
                        gpsTotalLocation[2 * i] = GlobalVariable.gpsAllLocation.Split('|')[i].Split(',')[0];
                        gpsTotalLocation[2 * i + 1] = GlobalVariable.gpsAllLocation.Split('|')[i].Split(',')[1];
                    }

                }
                var gpsJson = JsonConvert.SerializeObject(gpsTotalLocation);
                gpsJson.Trim(' ');
                Console.WriteLine(webBrowser1.Document.GetElementById("allGpsContainer").GetAttribute("value"));
                webBrowser1.Document.GetElementById("allGpsContainer").SetAttribute("value", gpsJson);
                Console.WriteLine(webBrowser1.Document.GetElementById("allGpsContainer").GetAttribute("value"));
                HtmlElement btnAdd = webBrowser1.Document.GetElementById("valueshot");
                btnAdd.InvokeMember("click");
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode.Nodes.Count == 0)
            {
                GlobalVariable.gpsPointedLocation = treeView1.SelectedNode.ToString().Substring(10);
                Console.WriteLine(treeView1.SelectedNode.ToString().Substring(10));
                if (webBrowser1.Document != null)
                {
                    webBrowser1.Document.GetElementById("PointedGpsContainer").SetAttribute("value", GlobalVariable.gpsPointedLocation);
                    HtmlElement btnShot = webBrowser1.Document.GetElementById("centreScreen");
                    btnShot.InvokeMember("click");

                }
            }
            
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
