using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace IcoTrans
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 程序打包后需要修改快捷方式地址  [TARGETDIR]应用程序名称.exe
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseFile(object sender, RoutedEventArgs e)
        {
            //这里需要先读取一个配置文件
            using (var stream = File.Open("directory.xml", FileMode.OpenOrCreate))
            {
                string dir = @"C:\Users\ZBS\Desktop";
                if (stream == null || stream.Length == 0)
                {
                    stream.Close();
                    SavePath(dir);
                }
                else
                {
                    stream.Close();
                    dir = GetPath();
                }
                OpenFileDialog dialog = new OpenFileDialog()
                {
                    InitialDirectory = dir,//设置文件打开初始目录为桌面
                    Title = "请选择图片",//设置打开文件对话框标题
                    Filter = "图片文件(*.jpg,*.gif,*.bmp,*.png)|*.jpg;*.gif;*.bmp;*.png",//设置文件过滤类型
                    RestoreDirectory = false //设置对话框是否记忆之前打开的目录
                };
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)//当点击文件对话框的确定按钮时打开相应的文件
                {
                    string FileName = dialog.FileName;
                    FileShow.Source = new BitmapImage(new Uri(FileName, UriKind.Absolute));//将选中的文件的路径传递给想应控件
                    FilePath.Content = FileName;
                    FileLabel.Content = FileName.Substring(FileName.LastIndexOf("\\") + 1);
                    SavePath(FileName.Substring(0, FileName.LastIndexOf("\\")));
                }
            }
        }

        public void SavePath(string dir)
        {
            XmlDocument xml = new XmlDocument();
            XmlElement config = xml.CreateElement("Config");
            xml.AppendChild(config);
            XmlElement path = xml.CreateElement("Path");
            path.InnerText = dir;
            config.AppendChild(path);
            xml.Save(@".\directory.xml");
        }

        public String GetPath()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(@".\directory.xml");
            return doc.InnerText;
        }

        //转换ico图片
        private void CreateICO(object sender, RoutedEventArgs e)
        {
            //byte[] imgData = Encoding.Default.GetBytes(FileShow.Source.ToString());
            try
            {
                string index = "ico";
                string path = FilePath.Content.ToString();
                string fname = FileLabel.Content.ToString();
                if (path == "")
                {
                    System.Windows.MessageBox.Show("请先选择图片");
                    return;
                }
                string[] size = System.Text.RegularExpressions.Regex.Split(FileSize.SelectedItem.ToString(), "x", RegexOptions.IgnoreCase);
                string bpath = path;
                string epath = GetPath() + "\\" + DateTime.Now.Ticks + "-" + fname.Substring(0, fname.LastIndexOf(".") + 1) + "ico";
                System.Windows.MessageBox.Show(Convert(bpath, epath, index, int.Parse(size[2]), int.Parse(size[2])));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public string Convert(string fileinpath, string fileoutpath, string index, int width, int height)
        {
            if (width <= 0 || height <= 0) return "error!size illegal!";
            try
            {
                Bitmap mybitmap = new Bitmap(fileinpath);
                Bitmap bitmap = new Bitmap(mybitmap, width, height);
                index = index.ToLower();
                switch (index)
                {
                    case "jpg": bitmap.Save(fileoutpath, ImageFormat.Jpeg); break;
                    case "jpeg": bitmap.Save(fileoutpath, ImageFormat.Jpeg); break;
                    case "bmp": bitmap.Save(fileoutpath, ImageFormat.Bmp); break;
                    case "png": bitmap.Save(fileoutpath, ImageFormat.Png); break;
                    case "emf": bitmap.Save(fileoutpath, ImageFormat.Emf); break;
                    case "gif": bitmap.Save(fileoutpath, ImageFormat.Gif); break;
                    case "wmf": bitmap.Save(fileoutpath, ImageFormat.Wmf); break;
                    case "exif": bitmap.Save(fileoutpath, ImageFormat.Exif); break;
                    case "tiff":
                        {
                            Stream stream = File.Create(fileoutpath);
                            bitmap.Save(stream, ImageFormat.Tiff);
                            stream.Close();
                        }
                        break;
                    case "ico":
                        {
                            if (height > 256 || width > 256)//ico maxsize 256*256
                                return "Error!Size illegal!";

                            Stream stream = File.Create(fileoutpath);
                            Icon ico = ConvertToIcon(bitmap);
                            ico.Save(stream);       //   save the icon
                            stream.Close();
                        }
                        break;
                    default: return "Error!";
                }
                return "Success!";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static Icon ConvertToIcon(System.Drawing.Image image, bool nullTonull = false)
        {
            if (image == null)
            {
                if (nullTonull) { return null; }
                throw new ArgumentNullException("image");
            }
            using (MemoryStream msImg = new MemoryStream()
                      , msIco = new MemoryStream())
            {
                image.Save(msImg, ImageFormat.Png);
                using (var bin = new BinaryWriter(msIco))
                {
                    //写图标头部
                    bin.Write((short)0);           //0-1保留
                    bin.Write((short)1);           //2-3文件类型。1=图标, 2=光标
                    bin.Write((short)1);           //4-5图像数量（图标可以包含多个图像）

                    bin.Write((byte)image.Width);  //6图标宽度
                    bin.Write((byte)image.Height); //7图标高度
                    bin.Write((byte)0);            //8颜色数（若像素位深>=8，填0。这是显然的，达到8bpp的颜色数最少是256，byte不够表示）
                    bin.Write((byte)0);            //9保留。必须为0
                    bin.Write((short)0);           //10-11调色板
                    bin.Write((short)32);          //12-13位深
                    bin.Write((int)msImg.Length);  //14-17位图数据大小
                    bin.Write(22);                 //18-21位图数据起始字节

                    //写图像数据
                    bin.Write(msImg.ToArray());

                    bin.Flush();
                    bin.Seek(0, SeekOrigin.Begin);
                    return new Icon(msIco);
                }
            }
        }
    }
}
