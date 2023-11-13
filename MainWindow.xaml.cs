using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using Microsoft.Win32;
using System.Net;
using System.Threading.Tasks;
using Squirrel;

namespace MathpixtoTeX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            LoadDSID();
            myListView.ItemsSource = chinhtas;
            gridChinhta.Width = new GridLength(30);
        }
        private UpdateManager updateManager;
        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Left = SystemParameters.WorkArea.Left;
            this.Top = SystemParameters.WorkArea.Top;
            this.Height = SystemParameters.WorkArea.Height;
            this.Width = SystemParameters.WorkArea.Width;
            btnUpdate.IsEnabled = false;
        }
        private async void CheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            await updateManager.UpdateApp();
            MessageBox.Show("Đã cập nhật chương trình thành công!", "Mathpix2TeX", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            updateManager = await UpdateManager.GitHubUpdateManager("https://github.com/overlordl2k/MathpixtoTeX");
            CurrentVersion.Content = updateManager.CurrentlyInstalledVersion().ToString();
            var updateInfo = await updateManager.CheckForUpdate();
            if (updateInfo.ReleasesToApply.Count > 0)
            {
                btnUpdate.IsEnabled = true;
            }
            else
            {
                btnUpdate.IsEnabled = false;
            }
        }
        private string GetApplicationFolderPath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
       
        public static string Codetho = String.Empty;
        public static ObservableCollection<Chinhta> chinhtas = new ObservableCollection<Chinhta>();
        public static void LoadDSID()
        {
            chinhtas.Clear();
            using (SQLiteConnection KetnoiDL = new SQLiteConnection("Data Source = MathpixData.db;Version=3; New=False;Compress=True;"))
            {
                KetnoiDL.Open();
                using (SQLiteCommand LenhKN = KetnoiDL.CreateCommand())
                {
                    LenhKN.CommandText = @"SELECT  STT, Timkiem, Thaythe FROM Chinhta ORDER BY STT";
                    SQLiteDataReader r = LenhKN.ExecuteReader();
                    while (r.Read())
                    {
                        chinhtas.Add(new Chinhta() { STT = Convert.ToInt32(r["STT"]), Timkiem = Convert.ToString(r["Timkiem"]), Thaythe = Convert.ToString(r["Thaythe"])});
                    }
                }
                KetnoiDL.Close();
            }
        }
        public void ThemloiCT()
        {
            string timKiemValue = txtTimkem.Text;
            string thayTheValue = txtThaythe.Text;
            using (SQLiteConnection KetnoiDL = new SQLiteConnection("Data Source = MathpixData.db;Version=3; New=False;Compress=True;"))
            {
                KetnoiDL.Open();
                using (SQLiteCommand LenhKN = KetnoiDL.CreateCommand())
                {
                    LenhKN.CommandText = @"INSERT INTO Chinhta (Timkiem, Thaythe) VALUES (@TimKiemValue, @ThayTheValue)";
                    LenhKN.Parameters.AddWithValue("@TimKiemValue", timKiemValue);
                    LenhKN.Parameters.AddWithValue("@ThayTheValue", thayTheValue);
                    LenhKN.Prepare();
                    try
                    {
                        chinhtas.Add(new Chinhta() { Timkiem = timKiemValue, Thaythe = thayTheValue });
                        LenhKN.ExecuteNonQuery();
                    }
                    catch
                    {
                        MessageBox.Show("Cụm từ: " + timKiemValue + "\nđã tồn tại trong ngân hàng CSDL", "MCP Test 2020", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                KetnoiDL.Close();
            }
        }
        private void Clipboard_Click(object sender, RoutedEventArgs e)
        {
            Codetho = "\n" + GetTextFromClipboard();            
            LoadND();
        }
        public void LoadND()
        {
            FlowDocument flowDoc = new FlowDocument();
            Span span = new Span(new Run(Codetho));
            span.FontSize = 18;
            Paragraph paragraph = new Paragraph(span);
            flowDoc.Blocks.Add(paragraph);
            Hienthicode.Document = flowDoc;
            Clipboard.Clear();
        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll")]
        public static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll")]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        public static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        public static extern bool CloseClipboard();

        public string GetTextFromClipboard()
        {
            string text = string.Empty;

            if (IsClipboardFormatAvailable(13)) // 13 corresponds to CF_UNICODETEXT
            {
                IntPtr hWnd = GetOpenClipboardWindow();

                if (OpenClipboard(hWnd))
                {
                    IntPtr clipboardData = GetClipboardData(13);
                    text = Marshal.PtrToStringUni(clipboardData);
                    CloseClipboard();
                }
            }

            return text;
        }
        private void ChuyenTeX_Click(object sender, RoutedEventArgs e)
        {
            if (Codetho.Length > 0)
            {
                try
                {
                    string ChuyenND = new TextRange(Hienthicode.Document.ContentStart, Hienthicode.Document.ContentEnd).Text + "\n\\end{ex}";
                    ChuyenND = Regex.Replace(ChuyenND, @"\r\n", "\n");
                    ChuyenND = Regex.Replace(ChuyenND, @"Chọn \$\\mathbf{(.*?)}\$", m => string.Format("{0}", "Chọn " + m.Groups[1].Value + "\n"));
                    ChuyenND = Regex.Replace(ChuyenND, @"\\section\*{(\d{1,}.*?)}\n", m => string.Format("{0}", "#EndCH#\n" + m.Groups[1].Value + "\n"));
                    ChuyenND = ChuyenND.Replace(@"\section*{BÀI", "#EndCH#\n\\section*{BÀI");
                    ChuyenND = Regex.Replace(ChuyenND, @"\\subsection\*{(\d{1,}.*?)}\n", m => string.Format("{0}", "#EndCH#\n" + m.Groups[1].Value + "\n"));
                    ChuyenND = Regex.Replace(ChuyenND, @"\\section\*{(.*?)}\n", m => string.Format("{0}", m.Groups[1].Value + "\n"));
                    ChuyenND = Regex.Replace(ChuyenND, @"Hướng dẫn \(Group Vật lý Physics\)", m => string.Format("{0}", "\\loigiai{\n"));
                    ChuyenND = Regex.Replace(ChuyenND, @"Lời giải([\s\.:])", m => string.Format("{0}", "\\loigiai{\n"));
                    ChuyenND = Regex.Replace(ChuyenND, @"(Câu \d{1,}[:.]\s{1,})", m => string.Format("{0}", "\n#EndCH#\n#BeginCH#\n"));
                    string Regexs = @"#BeginCH#(.|\n)*?#EndCH#";
                    foreach (Match match in Regex.Matches(ChuyenND, Regexs, RegexOptions.None))
                    {
                        if (match.Success)
                        {
                            string cauhoi = match.Value;
                            string caumoi = cauhoi;
                            caumoi = caumoi.Replace(@"#BeginCH#", @"\begin{ex}");
                            caumoi = caumoi.Replace(@"#EndCH#", @"\end{ex}");
                            ChuyenND = ChuyenND.Replace(match.Value, caumoi);
                            
                        }
                    }
                    ChuyenND = Regex.Replace(ChuyenND, @"A\.(.*?)\nB\.(.*?)\nC\.(.*?)\nD\.(.*?)\n", m => string.Format("{0}", "\\choice\n{"
                        + m.Groups[1].Value + "}\n{" + m.Groups[2].Value + "}\n{" + m.Groups[3].Value + "}\n{" + m.Groups[4].Value + "}\n"));
                    Regexs =  @"\\begin{ex}(.|\n)*?\\end{ex}";
                    foreach (Match match in Regex.Matches(ChuyenND, Regexs, RegexOptions.None))
                    {
                        if (match.Success)
                        {
                            string cauhoi = match.Value;
                            string caumoi = cauhoi;
                            caumoi = caumoi.Replace(@"\begin{ex}", "");
                            caumoi = caumoi.Replace(@"\end{ex}", "");
                            if (caumoi.Contains(@"\loigiai{"))
                            {
                                ChuyenND = ChuyenND.Replace(match.Value, "\\begin{ex}\n" + caumoi + "}\n\\end{ex}\n");
                            }
                            //MessageBox.Show(caumoi, "Mathpix2TeX", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    ChuyenND = Regex.Replace(ChuyenND, @"#EndCH#\n", "");
                    Codetho = ChuyenND;
                    LoadND();
                    Suachinhta();
                    HighlightKeywords();
                }
                catch {}
                
            }
            
        }
        private List<string> keywordList = new List<string> { @"\begin", @"\end", @"\choice", @"\subsection", @"\item" };
        private void HighlightKeywords()
        {
            TextRange textRange = new TextRange(Hienthicode.Document.ContentStart, Hienthicode.Document.ContentEnd);
            textRange.ClearAllProperties();
            foreach (var item in keywordList)
            {
                for (TextPointer startPointer = Hienthicode.Document.ContentStart;
                    startPointer.CompareTo(Hienthicode.Document.ContentEnd) <= 0;
                        startPointer = startPointer.GetNextContextPosition(LogicalDirection.Forward))
                {
                    //check if end of text
                    if (startPointer.CompareTo(Hienthicode.Document.ContentEnd) == 0)
                    {
                        break;
                    }

                    //get the adjacent string
                    string parsedString = startPointer.GetTextInRun(LogicalDirection.Forward);

                    //check if the search string present here
                    int indexOfParseString = parsedString.IndexOf(item);

                    if (indexOfParseString >= 0) //present
                    {
                        //setting up the pointer here at this matched index
                        startPointer = startPointer.GetPositionAtOffset(indexOfParseString);

                        if (startPointer != null)
                        {
                            //next pointer will be the length of the search string
                            TextPointer nextPointer = startPointer.GetPositionAtOffset(item.Length);

                            //create the text range
                            TextRange searchedTextRange = new TextRange(startPointer, nextPointer);

                            //color up 
                            searchedTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
                            searchedTextRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                            searchedTextRange.ApplyPropertyValue(TextElement.FontSizeProperty, FontSize = 18);
                            //add other setting property

                        }
                    }
                }
            }
        }
        private void Themloi_Click(object sender, RoutedEventArgs e)
        {
            ThemloiCT();
            myListView.ItemsSource = chinhtas;
        }
        private void Fixchinhta_Click(object sender, RoutedEventArgs e)
        {
            Suachinhta();
        }
        public void Suachinhta()
        {
            string ChuyenND = new TextRange(Hienthicode.Document.ContentStart, Hienthicode.Document.ContentEnd).Text;
            ChuyenND = Regex.Replace(ChuyenND, @"\r\n", "\n");
            ChuyenND = Regex.Replace(ChuyenND, @"\n{2,}", "\n");
            ChuyenND = Regex.Replace(ChuyenND, @"\r\n", "\n");
            ChuyenND = Regex.Replace(ChuyenND, @"\.}\n", "}\n");
            ChuyenND = Regex.Replace(ChuyenND, @"\n{2,}", "\n");
            ChuyenND = Regex.Replace(ChuyenND, @"\n\d{1,}\.(\d{1,})\*\s", m => string.Format("{0}", "\nCâu " + m.Groups[1].Value + ". "));
            ChuyenND = Regex.Replace(ChuyenND, @"\n\d{1,}\.(\d{1,})\*\.", m => string.Format("{0}", "\nCâu " + m.Groups[1].Value + ". "));
            ChuyenND = Regex.Replace(ChuyenND, @"\n\d{1,}\.(\d{1,}\.)", m => string.Format("{0}", "\nCâu " + m.Groups[1].Value));
            ChuyenND = Regex.Replace(ChuyenND, @"\s([}\)\.\?])", m => string.Format("{0}", "" + m.Groups[1].Value));
            ChuyenND = Regex.Replace(ChuyenND, @"([{\(])\s", m => string.Format("{0}", "" + m.Groups[1].Value));
            ChuyenND = Regex.Replace(ChuyenND, @"\^{([\d\w])}", m => string.Format("{0}", "^" + m.Groups[1].Value));
            ChuyenND = Regex.Replace(ChuyenND, @"_{([\d\w])}", m => string.Format("{0}", "_" + m.Groups[1].Value));
            ChuyenND = Regex.Replace(ChuyenND, @"\n[\s\t]", "\n");
            ChuyenND = Regex.Replace(ChuyenND, @"\\mathrm{(.*?)}", m => string.Format("{0}", "" + m.Groups[1].Value));
            ChuyenND = Regex.Replace(ChuyenND, @"\(\\item\s\n", @"\item ");
            foreach (var item in chinhtas)
            {
                ChuyenND = ChuyenND.Replace(item.Timkiem, item.Thaythe);
            }

            ChuyenND = Regex.Replace(ChuyenND, @"\.}\n", "}\n");
            ChuyenND = Regex.Replace(ChuyenND, @"\n{2,}", "\n");
            Codetho = ChuyenND;
            LoadND();
            
        }    
        private void XoaCSDL_Click(object sender, RoutedEventArgs e)
        {
            var item = (Chinhta)myListView.SelectedItem;
            if (item != null)
            {
                using (SQLiteConnection KetnoiDL = new SQLiteConnection("Data Source = MathpixData.db;Version=3; New=False;Compress=True;"))
                {
                    KetnoiDL.Open();
                    using (SQLiteCommand LenhKN = KetnoiDL.CreateCommand())
                    {
                        LenhKN.CommandText = @"DELETE FROM Chinhta WHERE STT = @ItemId";
                        LenhKN.Parameters.AddWithValue("@ItemId", item.STT);
                        LenhKN.Prepare();
                        try
                        {
                            LenhKN.ExecuteNonQuery();
                        }
                        catch
                        {
                            MessageBox.Show("Cụm từ: " + item.Timkiem + "\nđã xóa trong ngân hàng CSDL", "Mathpix2TeX", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    KetnoiDL.Close();
                }
                LoadDSID();
                myListView.ItemsSource = chinhtas;
            }
        }
        private void LoadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Cấu hình dialog
            openFileDialog.Title = "Chọn tập tin"; // Tiêu đề của dialog
            openFileDialog.Filter = "Tất cả các tập tin|*.tex"; // Bộ lọc tập tin, ở đây là tất cả các tập tin

            // Mở dialog và kiểm tra xem người dùng đã chọn tập tin hay chưa
            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                Codetho = File.ReadAllText(openFileDialog.FileName);
            }
            LoadND();
        }
        [STAThread]
        private void Saochep_Click(object sender, RoutedEventArgs e)
        {
            Hienthicode.SelectAll();
            Hienthicode.Copy();
        }
        private void Xoacode_Click(object sender, RoutedEventArgs e)
        {
            Hienthicode.SelectAll();
            Hienthicode.Document.Blocks.Clear();
        }
        private void TaoEnumEX_Click(object sender, RoutedEventArgs e)
        {
            string dang = ((ComboBoxItem)Dang.SelectedItem).Content.ToString();
            string cot = ((ComboBoxItem)Socot.SelectedItem).Content.ToString();
            string Noidung = Hienthicode.Selection.Text + "\n";
            Noidung = Regex.Replace(Noidung, @"\r\n", "\n");
            Noidung = Regex.Replace(Noidung, @"\n{2,}", "\n");
            //Noidung = Regex.Replace(Noidung, @"\(([\w\d][\.\)])(.*?)\n", m => string.Format("{0}", "" + "\n\\item " + m.Groups[2].Value + "\n"));
            Noidung = Regex.Replace(Noidung, @"([\w\d][\.\)])(.*?)\n", m => string.Format("{0}", "" + "\\item " + m.Groups[2].Value + "\n"));
            Noidung = "\\begin{enumEX}[" + dang + "]{" + cot + "}\n" + Noidung + "\n\\end{enumEX}\n";            
            Noidung = Regex.Replace(Noidung, @"\r\n", "\n");
            Noidung = Regex.Replace(Noidung, @"\n{2,}", "\n");
            Noidung = Regex.Replace(Noidung, @"\(\\item", @"\item ");
            Noidung = Regex.Replace(Noidung, @"\\item\s\n", @"\item ");
            Noidung = Regex.Replace(Noidung, @"\n[\s\t]", "\n");
            Hienthicode.Selection.Text = Noidung;
            //Suachinhta();
        }
        private void TaoListEX_Click(object sender, RoutedEventArgs e)
        {
            string cot = ((ComboBoxItem)Socot.SelectedItem).Content.ToString();
            string Noidung = Hienthicode.Selection.Text + "\n";
            Noidung = Regex.Replace(Noidung, @"\r\n", "\n");
            Noidung = Regex.Replace(Noidung, @"\n{2,}", "\n");
            //Noidung = Regex.Replace(Noidung, @"\(([\w\d][\.\)])(.*?)\n", m => string.Format("{0}", "" + "\n\\item " + m.Groups[2].Value + "\n"));
            Noidung = Regex.Replace(Noidung, @"([\w\d][\.\)])(.*?)\n", m => string.Format("{0}", "" + "\\item " + m.Groups[2].Value + "\n"));
            Noidung = "\\begin{listEX}{" + cot + "}\n" + Noidung + "\n\\end{listEX}\n";
            Noidung = Regex.Replace(Noidung, @"\r\n", "\n");
            Noidung = Regex.Replace(Noidung, @"\n{2,}", "\n");
            Noidung = Regex.Replace(Noidung, @"\(\\item", @"\item ");
            Noidung = Regex.Replace(Noidung, @"\\item\s\n", @"\item ");
            Noidung = Regex.Replace(Noidung, @"\n[\s\t]", "\n");
            Hienthicode.Selection.Text = Noidung;
            //Suachinhta();
        }
        private void EXsangBT_Click(object sender, RoutedEventArgs e)
        {
            string Noidung = Hienthicode.Selection.Text;
            Noidung = Regex.Replace(Noidung, @"{ex}", "{bt}");
            Noidung = Regex.Replace(Noidung, @"\n{2,}", "\n");
            Hienthicode.Selection.Text = Noidung;
            Hienthicode.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
            Hienthicode.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            ChangeTextColorInRichTextBox();
        }
        private void ChangeTextColorInRichTextBox()
        {
            TextPointer position = Hienthicode.Document.ContentStart; // Bắt đầu từ đầu văn bản

            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);

                    int index = textRun.IndexOf("\\begin{ex}");
                    if (index >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(index);
                        TextPointer end = start.GetPositionAtOffset("\\begin{ex}".Length);
                        ChangeTextColor(start, end, Colors.Blue); // Đổi màu văn bản từ \begin{ex}
                    }
                    index = textRun.IndexOf("\\begin{bt}");
                    if (index >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(index);
                        TextPointer end = start.GetPositionAtOffset("\\begin{bt}".Length);
                        ChangeTextColor(start, end, Colors.Blue); // Đổi màu văn bản từ \begin{ex}
                    }
                    index = textRun.IndexOf("\\end{ex}");
                    if (index >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(index+3);
                        TextPointer end = start.GetPositionAtOffset("\\end{ex}".Length);
                        ChangeTextColor(start, end, Colors.Blue); // Đổi màu văn bản từ \end{ex}
                    }
                    index = textRun.IndexOf("\\end{bt}");
                    if (index >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(index + 1);
                        TextPointer end = start.GetPositionAtOffset("\\end{bt}".Length);
                        ChangeTextColor(start, end, Colors.Blue); // Đổi màu văn bản từ \end{ex}
                    }
                    index = textRun.IndexOf("\\choice");
                    if (index >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(index + 3);
                        TextPointer end = start.GetPositionAtOffset("\\choice".Length);
                        ChangeTextColor(start, end, Colors.Red); // Đổi màu văn bản từ \end{ex}
                    }
                }
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        private void ChangeTextColor(TextPointer start, TextPointer end, Color color)
        {
            TextRange textRange = new TextRange(start, end);
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            textRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            double newWidth = 400;
            if (DongmoCT.IsChecked == true)
            {
                gridChinhta.Width = new GridLength(newWidth);
            }
            else
            {
                gridChinhta.Width = new GridLength(30);
            }
            
        }
        private void InsertTextAtPosition(string text, TextPointer position)
        {
            TextRange textRange = new TextRange(position, position);
            textRange.Text = text;
        }
        private void Themdong_Click(object sender, RoutedEventArgs e)
        {
            if (Nhapsd.Text.Trim()!="")
            {
                TextPointer insertionPosition = Hienthicode.CaretPosition;
                InsertTextAtPosition(@"\dotlineEX{" + Nhapsd.Text.Trim() + "}\n", insertionPosition);
            }
        }

        private void Taodemuc_Click(object sender, RoutedEventArgs e)
        {
            string selectedValue = ((ComboBoxItem)Chapter.SelectedItem).Content.ToString();
            if (selectedValue != null)
            {
                string selectedText = selectedValue;
                string Noidung = "\\" + selectedText + "{" + Hienthicode.Selection.Text + "}";
                Hienthicode.Selection.Text = Noidung;
                Hienthicode.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                Hienthicode.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            }
            else
            {
                MessageBox.Show("Nội dung chưa được chọn", "Mathpix2TeX", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public int STTdl;
        private void Suacumtu_Click(object sender, RoutedEventArgs e)
        {
            using (SQLiteConnection KetnoiDL = new SQLiteConnection("Data Source = MathpixData.db;Version=3; New=False;Compress=True;"))
            {
                KetnoiDL.Open();
                using (SQLiteCommand LenhKN = KetnoiDL.CreateCommand())
                {
                    LenhKN.CommandText = @"UPDATE Chinhta SET Timkiem = @TimKiemValue, Thaythe = @ThayTheValue WHERE STT = @ID";
                    LenhKN.Parameters.AddWithValue("@TimKiemValue", txtTimkem.Text.Trim());
                    LenhKN.Parameters.AddWithValue("@ThayTheValue", txtThaythe.Text.Trim());
                    LenhKN.Parameters.AddWithValue("@ID", STTdl);
                    LenhKN.Prepare();
                    try
                    {
                        LenhKN.ExecuteNonQuery();
                    }
                    catch
                    {
                        MessageBox.Show("Cập nhật cụm từ vào CSDL thất bại!", "MCP Test 2020", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                KetnoiDL.Close();
            }
            LoadDSID();
        }

        private void myListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ((Chinhta)myListView.SelectedItem);
            if (item != null)
            {
                STTdl = item.STT;
                txtTimkem.Text = item.Timkiem;
                txtThaythe.Text = item.Thaythe;
            }
        }
        
    }
    
    public class Chinhta : INotifyPropertyChanged
    {
        public int _STT;
        public int STT
        {
            get
            {
                return _STT;
            }
            set
            {
                if (_STT != value)
                {
                    _STT = value;
                    NotifyPropertyChanged("STT");
                }
            }
        }
        public string _Timkiem;
        public string Timkiem
        {
            get
            {
                return _Timkiem;
            }
            set
            {
                if (_Timkiem != value)
                {
                    _Timkiem = value;
                    NotifyPropertyChanged("Timkiem");
                }
            }
        }
        public string _Thaythe;
        public string Thaythe
        {
            get
            {
                return _Thaythe;
            }
            set
            {
                if (_Thaythe != value)
                {
                    _Thaythe = value;
                    NotifyPropertyChanged("Thaythe");
                }
            }
        }        
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
