using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.VBA;

[assembly: AssemblyTitle("교육공무직원 320일 근무 편성기")]
[assembly: AssemblyProduct("교육공무직원 320일 근무 편성기")]
[assembly: AssemblyDescription("방중비 교육공무직원의 320일 근무 편성 기초일수 계산을 돕는 프로그램")]
[assembly: AssemblyVersion(SchoolWorkdayCalculator.AppRelease.FileVersion)]
[assembly: AssemblyFileVersion(SchoolWorkdayCalculator.AppRelease.FileVersion)]

namespace SchoolWorkdayCalculator
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;
            if (args.Length >= 7 && args[0] == "--generate")
            {
                try
                {
                    int year = Int32.Parse(args[1], CultureInfo.InvariantCulture);
                    Settings settings = new Settings
                    {
                        Year = year,
                        Summer = new DateRange(DateTime.ParseExact(args[3], "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTime.ParseExact(args[4], "yyyy-MM-dd", CultureInfo.InvariantCulture), "여름방학"),
                        Winter = new DateRange(DateTime.ParseExact(args[5], "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTime.ParseExact(args[6], "yyyy-MM-dd", CultureInfo.InvariantCulture), "겨울방학"),
                        Discretionary = new List<DateTime>(), DiscretionaryNames = new Dictionary<DateTime, string>(), Extras = new List<ExtraHoliday>(), OutputFolder = Path.GetDirectoryName(args[2])
                    };
                    if (args.Length >= 9 && !String.IsNullOrWhiteSpace(args[8]))
                    {
                        foreach (string item in args[8].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string[] pair = item.Split(new[] { '=' }, 2); DateTime date = DateTime.ParseExact(pair[0], "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
                            settings.Discretionary.Add(date); settings.DiscretionaryNames[date] = pair.Length > 1 ? pair[1].Trim() : "";
                        }
                    }
                    CalculationResult result = WorkbookBuilder.Build(args[2], settings);
                    if (args.Length >= 8) File.WriteAllText(args[7], String.Format(CultureInfo.InvariantCulture, "REGULAR_WORK={0}\r\nREGULAR_PAID={1}\r\nSEASONAL_WORK={2}\r\nSEASONAL_PAID={3}\r\n", result.RegularWorkDays, result.RegularPaidDays, result.SeasonalWorkDays, result.SeasonalPaidDays));
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    if (args.Length >= 8) File.WriteAllText(args[7], "ERROR\r\n" + ex);
                    Environment.Exit(1);
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length>=2 && args[0]=="--policy1114-test"){Policy1114Tests.Run(args[1]);return;}
            if(args.Length>=2 && (args[0]=="--update-test" || args[0]=="--update-live-test")){AppUpdateTests.Run(args[1],args[0]=="--update-live-test");return;}
            if(args.Length>=2 && args[0]=="--flat-test"){MainForm.TestFlat(args[1]);return;}
            if(args.Length>=2 && args[0]=="--nonconsent-test"){MainForm.TestNonConsent(args[1]);return;}
            if(args.Length>=2 && (args[0]=="--holiday-test" || args[0]=="--holiday-live-test")){MainForm.TestHolidays(args[1],args[0]=="--holiday-live-test");return;}
            Application.Run(new MainForm());
        }

        private static Assembly ResolveEmbeddedAssembly(object sender, ResolveEventArgs args)
        {
            if (!new AssemblyName(args.Name).Name.Equals("EPPlus", StringComparison.OrdinalIgnoreCase)) return null;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SchoolWorkdayCalculator.EPPlus.dll"))
            {
                if (stream == null) return null;
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return Assembly.Load(bytes);
            }
        }
    }

    internal sealed partial class MainForm : Form
    {
        private readonly StepBox year = new StepBox();
        private readonly DateBox summerStart = DatePicker();
        private readonly DateBox summerEnd = DatePicker();
        private readonly DateBox winterStart = DatePicker();
        private readonly DateBox winterEnd = DatePicker();
        private readonly DateBox[] discretionary = new DateBox[4];
        private readonly TextBox[] discretionaryNotes = new TextBox[4];
        private readonly List<ExtraHoliday> extras = new List<ExtraHoliday>();
        private readonly ListBox extraList = new PastelListBox();
        private readonly TextBox outputFolder = new TextBox();
        private readonly Label status = new Label();
        private readonly Button createButton = new PastelButton();

        public MainForm()
        {
            BuildFlatInterface();
            SetDefaults();
            InitializeHolidayUpdates();
            outputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            InitializeLivePreview();
        }

        private static Icon LoadAppIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SchoolWorkdayCalculator.AppIcon.ico"))
                return stream == null ? null : (Icon)new Icon(stream).Clone();
        }

        private static DateBox DatePicker()
        {
            return new DateBox();
        }

        private static Label LabelAt(string text, int x, int y)
        {
            return new Label { Text = text, AutoSize = true, Location = new Point(x, y) };
        }

        private static Button ButtonAt(string text, int x, int y, int width, int height)
        {
            Button button = new PastelButton { Text = text };
            FlatDesign.Button(button, false);
            button.SetBounds(x, y, width, height);
            return button;
        }

        private static void AddDateRange(Control parent, string caption, DateBox start, DateBox end, int x, int y)
        {
            parent.Controls.Add(LabelAt(caption + " 시작일", x, y + 5));
            start.SetBounds(x + 112, y, 180, 27);
            parent.Controls.Add(start);
            parent.Controls.Add(LabelAt("종료일", x + 330, y + 5));
            end.SetBounds(x + 395, y, 180, 27);
            parent.Controls.Add(end);
        }

        private void SetDefaults()
        {
            guardBusy = true;
            agreedWork.Clear();
            int y = (int)year.Value;
            summerStart.Value = new DateTime(y, 7, 21);
            summerEnd.Value = new DateTime(y, 8, 2);
            winterStart.Value = new DateTime(y, 12, 21);
            winterEnd.Value = new DateTime(y + 1, 2, DateTime.DaysInMonth(y + 1, 2));
            for (int i = 0; i < discretionary.Length; i++)
            {
                discretionary[i].MinDate = DateBox.MinimumDateTime;
                discretionary[i].MaxDate = new DateTime(y + 1, 2, DateTime.DaysInMonth(y + 1, 2));
                discretionary[i].MinDate = new DateTime(y, 3, 1);
                discretionary[i].Value = new DateTime(y, 5, Math.Min(1 + i, 28));
                discretionary[i].Checked = false;
                discretionaryNotes[i].Text = "";
            }
            extras.Clear(); RefreshExtras();
            guardBusy = false;
        }

        private void ChooseFolder(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog { Description = "결과 엑셀을 저장할 폴더를 선택하세요.", SelectedPath = Directory.Exists(outputFolder.Text) ? outputFolder.Text : "" })
                if (dialog.ShowDialog() == DialogResult.OK) outputFolder.Text = dialog.SelectedPath;
        }

        private void AddExtra(object sender, EventArgs e)
        {
            int y = (int)year.Value;
            using (ExtraHolidayDialog dialog = new ExtraHolidayDialog(new DateTime(y, 3, 1), new DateTime(y + 1, 2, DateTime.DaysInMonth(y + 1, 2))))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                extras.Add(new ExtraHoliday { Date = dialog.SelectedDate, Name = dialog.HolidayName });
                extras.Sort((a, b) => a.Date.CompareTo(b.Date));
                RefreshExtras();
            }
        }

        private void RemoveExtra(object sender, EventArgs e)
        {
            if (extraList.SelectedIndex < 0) return;
            extras.RemoveAt(extraList.SelectedIndex);
            RefreshExtras();
        }

        private void RefreshExtras()
        {
            if(extraEmpty!=null)extraEmpty.Visible=extras.Count==0;
            QueuePreview();
            extraList.Items.Clear();
            foreach (ExtraHoliday item in extras) extraList.Items.Add(item.Date.ToString("yyyy-MM-dd") + "  " + item.Name);
        }

        private void CreateWorkbook(object sender, EventArgs e)
        {
            try
            {
                createButton.Enabled = false;
                status.Text = "달력과 근로일수를 계산하고 있습니다...";
                Refresh();
                Settings settings = BuildSettings();
                Directory.CreateDirectory(settings.OutputFolder);
                string path = ResolveOutputPath(Path.Combine(settings.OutputFolder, settings.Year + (settings.NonConsent?"학년도_320일비동의자_근로일수_산정.xlsx":"학년도_교육공무직_근로일수_산정.xlsx")));
                if(String.IsNullOrWhiteSpace(path)){status.Text="파일 생성을 취소했습니다.";return;}
                CalculationResult result = WorkbookBuilder.Build(path, settings);
                status.Text = "완료: " + Path.GetFileName(path);
                string message = "엑셀 파일을 생성했습니다.\r\n\r\n" +
                    "상시근로자 소정근로일수: " + result.RegularWorkDays + "일\r\n" +
                    (settings.NonConsent?"비동의자 소정근로일수: ":"방중비 소정근로일수: ") + result.SeasonalWorkDays + "일\r\n" +
                    (settings.NonConsent?"비동의자 유급 산정일수: ":"방중비 유급 산정일수: ") + result.SeasonalPaidDays + "일";
                if (!settings.NonConsent && result.SeasonalPaidDays < 320) message += "\r\n\r\n※ 320일 기준보다 " + (320 - result.SeasonalPaidDays) + "일 부족합니다. 방학 중 추가 근무일을 검토해 주세요.";
                message += "\r\n\r\n저장 위치:\r\n" + path;
                Notify(message, "생성 완료", !settings.NonConsent && result.SeasonalPaidDays < 320 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                status.Text = "생성하지 못했습니다.";
                Notify(ex.Message, "오류", MessageBoxIcon.Error);
            }
            finally { createButton.Enabled = true; }
        }

        private string ResolveOutputPath(string defaultPath)
        {
            if(!File.Exists(defaultPath))return defaultPath;
            using(ExistingFileDialog dialog=new ExistingFileDialog(defaultPath))
            {
                DialogResult result=dialog.ShowDialog(this);
                if(result==DialogResult.Yes)return defaultPath;
                if(result!=DialogResult.Retry)return null;
            }
            using(SaveFileDialog save=new SaveFileDialog{Title="변경할 파일명과 저장 위치를 선택하세요.",Filter="Excel 통합문서 (*.xlsx)|*.xlsx",InitialDirectory=Path.GetDirectoryName(defaultPath),FileName=Path.GetFileNameWithoutExtension(defaultPath)+"_변경",DefaultExt="xlsx",AddExtension=true,OverwritePrompt=true})
                return save.ShowDialog(this)==DialogResult.OK?save.FileName:null;
        }

        private Settings BuildSettings(bool requireOutput = true)
        {
            int y = (int)year.Value;
            DateTime fiscalStart = new DateTime(y, 3, 1);
            DateTime fiscalEnd = new DateTime(y + 1, 2, DateTime.DaysInMonth(y + 1, 2));
            DateRange summer = new DateRange(summerStart.Value.Date, summerEnd.Value.Date, "여름방학");
            DateRange winter = new DateRange(winterStart.Value.Date, winterEnd.Value.Date, "겨울방학");
            if (!summer.IsValid || !winter.IsValid) throw new Exception("방학 종료일은 시작일보다 빠를 수 없습니다.");
            if (summer.Start < fiscalStart || summer.End > fiscalEnd || winter.Start < fiscalStart || winter.End > fiscalEnd) throw new Exception("방학 기간은 선택한 학교회계연도 안에 있어야 합니다.");
            if (summer.Overlaps(winter)) throw new Exception("여름방학과 겨울방학 기간이 겹칩니다.");
            List<DateTime> discretionaryDates = new List<DateTime>();Dictionary<DateTime,string> discretionaryNames = new Dictionary<DateTime,string>();
            for(int i=0;i<discretionary.Length;i++)if(discretionary[i].Checked){DateTime date=discretionary[i].Value.Date;if(!discretionaryDates.Contains(date))discretionaryDates.Add(date);string note=discretionaryNotes[i].Text.Trim();if(note.Length>0)discretionaryNames[date]=note;}
            CalendarPolicy.ValidateDiscretionary(y,discretionaryDates);
            if (requireOutput && String.IsNullOrWhiteSpace(outputFolder.Text)) throw new Exception("결과 저장 폴더를 선택해 주세요.");
            Settings built = new Settings { NonConsent = nonConsentMode, Year = y, Summer = summer, Winter = winter, Discretionary = discretionaryDates, DiscretionaryNames = discretionaryNames, Extras = extras.ToList(), AgreedWork = new HashSet<DateTime>(agreedWork), OutputFolder = outputFolder.Text.Trim() };
            CalendarPolicy.ValidateDiscretionaryOutsideVacation(built);
            return built;
        }

        private void OpenFolder(object sender, EventArgs e)
        {
            if (!Directory.Exists(outputFolder.Text)) { MessageBox.Show("저장 폴더를 먼저 선택해 주세요."); return; }
            Process.Start("explorer.exe", outputFolder.Text);
        }

        private void ShowHelp(object sender, EventArgs e)
        {
            MessageBox.Show(
                "1. 학교회계연도, 여름·겨울방학 기간을 입력합니다.\r\n" +
                "2. 달력의 날짜를 오른쪽 클릭해 재량휴업일·임시공휴일·약정근무일을 지정합니다.\r\n" +
                "3. 법정공휴일·대체공휴일·노동절은 공식 자료로 자동 반영합니다.\r\n" +
                "4. [근로일수 계산 및 엑셀 생성]을 누르면 상시근로자·방중비(또는 비동의자)·소정근로일표 시트를 만듭니다.\r\n\r\n" +
                "• 동의자는 방학 중 첫 3개·마지막 5개 평일을 근무일로 자동 편성합니다.\r\n" +
                "• 방학 중 근무일이 있는 주의 토·일은 유급이며, 공휴일만 있는 주에는 토·일이 유급이 되지 않습니다.\r\n" +
                "• 재량휴업일은 방학 기간 밖의 평일에만 지정할 수 있고, 4일 미만이면 부족한 수만큼 학습휴가로 차감합니다.",
                "사용방법", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    internal sealed class ExistingFileDialog : Form
    {
        public ExistingFileDialog(string path)
        {
            Text = "동일 파일 확인";
            ClientSize = new Size(570, 205);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("맑은 고딕", 9F);

            Label message = new Label
            {
                AutoSize = false,
                Location = new Point(22, 18),
                Size = new Size(525, 105),
                Text = "같은 이름의 파일이 이미 있습니다.\r\n\r\n" + Path.GetFileName(path) +
                       "\r\n\r\n예: 기존 파일 덮어쓰기    아니오: 생성 취소    변경 저장: 다른 이름으로 저장"
            };
            Controls.Add(message);

            Button yes = new Button { Text = "예", DialogResult = DialogResult.Yes };
            Button no = new Button { Text = "아니오", DialogResult = DialogResult.No };
            Button change = new Button { Text = "변경 저장", DialogResult = DialogResult.Retry };
            yes.SetBounds(270, 148, 82, 34);
            no.SetBounds(358, 148, 82, 34);
            change.SetBounds(446, 148, 100, 34);
            Controls.Add(yes);
            Controls.Add(no);
            Controls.Add(change);
            AcceptButton = yes;
            CancelButton = no;
            FlatDesign.Dialog(this);
        }
    }

    internal sealed class ExtraHolidayDialog : Form
    {
        private readonly DateBox date = new DateBox();
        private readonly TextBox name = new TextBox();
        public DateTime SelectedDate { get { return date.Value.Date; } }
        public string HolidayName { get { return String.IsNullOrWhiteSpace(name.Text) ? "기타 휴일" : name.Text.Trim(); } }

        public ExtraHolidayDialog(DateTime min, DateTime max)
        {
            Text = "기타 휴일 추가"; ClientSize = new Size(360, 150); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; Font = new Font("맑은 고딕", 9F);
            Controls.Add(new Label { Text = "날짜", AutoSize = true, Location = new Point(18, 22) });
            date.SetBounds(92, 14, 230, 34); date.MinDate = min; date.MaxDate = max; date.Value = min;
            Controls.Add(date);
            Controls.Add(new Label { Text = "휴일명", AutoSize = true, Location = new Point(18, 61) });
            name.SetBounds(92, 57, 230, 27); Controls.Add(name);
            Button ok = new Button { Text = "추가", DialogResult = DialogResult.OK }; ok.SetBounds(166, 101, 75, 32);
            Button cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel }; cancel.SetBounds(247, 101, 75, 32);
            Controls.Add(ok); Controls.Add(cancel); AcceptButton = ok; CancelButton = cancel; FlatDesign.Dialog(this);
        }
    }

    internal static class WorkbookBuilder
    {
        private static readonly int[] BlockRows = { 3, 12, 20, 29, 37, 46 };
        private static readonly Color RegularWorkFill = Color.FromArgb(198, 224, 180);
        private static readonly Color SaturdayFill = Color.FromArgb(91, 155, 213);
        private static readonly Color SundayFill = Color.FromArgb(244, 176, 176);
        private static readonly Color HolidayFill = Color.FromArgb(255, 235, 156);
        private static readonly Color VacationFill = Color.FromArgb(242, 242, 242);
        private static readonly Color VacationWorkFill = Color.FromArgb(244, 177, 131);
        internal const string PrescribedWorkdaySheet = "소정근로일표";

        public static CalculationResult Build(string destination, Settings settings)
        {
            byte[] template;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SchoolWorkdayCalculator.Template.xlsx"))
            {
                if (stream == null) throw new Exception("내장 엑셀 양식을 읽지 못했습니다.");
                template = new byte[stream.Length]; stream.Read(template, 0, template.Length);
            }

            Dictionary<DateTime,string> holidays;HashSet<DateTime> vacationWork;
            Prepare(settings,out holidays,out vacationWork);

            using (MemoryStream source = new MemoryStream(template))
            using (ExcelPackage package = new ExcelPackage(source))
            {
                ExcelWorksheet regular = package.Workbook.Worksheets[1];
                ExcelWorksheet seasonal = package.Workbook.Worksheets[2];
                regular.Name = "상시근로자";
                seasonal.Name = settings.NonConsent ? "320일 비동의자" : "방중비 근로자";
                // Clone the pristine template: EPPlus cannot clone cleared/shared formula cells reliably.
                SheetTotals regularTotals = FillSheet(regular, settings, holidays, vacationWork, false);
                SheetTotals seasonalTotals = FillSheet(seasonal, settings, holidays, vacationWork, true);
                package.Workbook.Properties.Comments="공휴일 출처: "+Holidays.SourceUrl+"\n원천 확인: "+Holidays.Current.fetchedAtUtc+"\n자료 버전: "+Holidays.Current.contentVersion;
                AddPrescribedWorkdaySheet(package, regular, seasonal, settings);
                package.Workbook.CalcMode = ExcelCalcMode.Automatic;
                package.Workbook.FullCalcOnLoad = true;
                package.SaveAs(new FileInfo(destination));
                return new CalculationResult { RegularWorkDays = regularTotals.WorkDays, RegularPaidDays = regularTotals.PaidDays, SeasonalWorkDays = seasonalTotals.WorkDays, SeasonalPaidDays = seasonalTotals.PaidDays };
            }
        }

        private static void AddPrescribedWorkdaySheet(ExcelPackage package, ExcelWorksheet regular, ExcelWorksheet seasonal, Settings settings)
        {
            var sheet = package.Workbook.Worksheets.Add(PrescribedWorkdaySheet);
            var calendars = new[] { regular, seasonal };
            var labels = new[] { "상시근로자", settings.NonConsent ? "320일 비동의자" : "방중비 근로자" };
            DateTime start = new DateTime(settings.Year, 3, 1);
            int dayCount = (start.AddYears(1) - start).Days;
            int lastRow = dayCount + 1;
            int learningDays = 4 - CalendarPolicy.DiscretionaryCount(settings);
            sheet.Cells[1, 1].Value = "날짜";
            sheet.Column(1).Width = 15;
            sheet.Cells[2, 1, lastRow, 1].Style.Numberformat.Format = "yyyy-mm-dd";
            sheet.Cells[2, 2, lastRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Cells[1, 1, 1, 3].Style.Font.Bold = true;
            sheet.Cells[1, 1, 1, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[1, 1, 1, 3].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(219, 234, 254));
            sheet.Cells[1, 1, lastRow, 3].AutoFilter = true;
            sheet.View.FreezePanes(2, 2);

            for (int i = 0; i < dayCount; i++)
                sheet.Cells[i + 2, 1].Value = start.AddDays(i).ToOADate();

            for (int i = 0; i < calendars.Length; i++)
            {
                int column = i + 2;
                sheet.Cells[1, column].Value = labels[i];
                sheet.Column(column).Width = 20;
                string learningRange = null;
                if (learningDays > 0)
                {
                    int inputColumn = i + 5;
                    sheet.Column(inputColumn).Width = 23;
                    sheet.Cells[4, inputColumn].Value = labels[i];
                    sheet.Cells[4, inputColumn].Style.Font.Bold = true;
                    var inputs = sheet.Cells[5, inputColumn, 4 + learningDays, inputColumn];
                    inputs.Style.Numberformat.Format = "yyyy-mm-dd";
                    inputs.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    inputs.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(254, 249, 195));
                    learningRange = ExcelCellBase.GetAddress(5, inputColumn, 4 + learningDays, inputColumn);
                }

                for (int day = 0; day < dayCount; day++)
                {
                    int row = day + 2;
                    string address = CalendarCodeAddress(start.AddDays(day));
                    string code = "'" + calendars[i].Name + "'!" + address;
                    string isWorkday = "OR(" + code + "=\"W\"," + code + "=\"A\"," + code + "=\"L\")";
                    if (learningRange != null)
                        isWorkday = "AND(" + isWorkday + ",COUNTIF(" + learningRange + ",A" + row + ")=0)";
                    sheet.Cells[row, column].Formula = "IF(" + isWorkday + ",1,0)";
                    // Cache the initial value for readers that do not recalculate the workbook.
                    sheet.SetValue(row, column, IsPrescribedWorkday(calendars[i].Cells[address].GetValue<string>()) ? 1 : 0);
                }
            }

            sheet.Cells[1, 5].Value = "1 = 소정근로일, 0 = 근로의무 없는 날";
            sheet.Cells[2, 5].Value = "달력 수정은 프로그램에서 합니다.";
            if (learningDays > 0)
                sheet.Cells[3, 5].Value = "학습휴가 날짜 입력 · 실제 적용일만 최대 " + learningDays + "일";
        }

        private static string CalendarCodeAddress(DateTime date)
        {
            int monthIndex = (date.Month + 9) % 12;
            int position = (int)new DateTime(date.Year, date.Month, 1).DayOfWeek + date.Day - 1;
            int helperColumn = monthIndex < 6 ? 27 : 34;
            return ExcelCellBase.GetAddress(BlockRows[monthIndex % 6] + 2 + position / 7, helperColumn + position % 7);
        }

        private static bool IsPrescribedWorkday(string code)
        {
            return code == "W" || code == "A" || code == "L";
        }

        private static void Prepare(Settings settings,out Dictionary<DateTime,string> holidays,out HashSet<DateTime> vacationWork)
        {
            CalendarPolicy.ValidateDiscretionary(settings.Year,settings.Discretionary);
            CalendarPolicy.ValidateDiscretionaryOutsideVacation(settings);
            holidays = HolidayCalculator.GetFiscalHolidays(settings.Year);
            foreach (DateTime date in settings.Discretionary)
            {
                string note;string name=settings.DiscretionaryNames!=null&&settings.DiscretionaryNames.TryGetValue(date,out note)&&!String.IsNullOrWhiteSpace(note)?"재량휴업일("+note.Trim()+")":"재량휴업일";
                holidays[date] = AppendName(holidays, date, name);
            }
            foreach (ExtraHoliday item in settings.Extras) holidays[item.Date] = AppendName(holidays, item.Date, item.Name);

            vacationWork = new HashSet<DateTime>();
            foreach (DateRange range in settings.NonConsent ? new DateRange[0] : new[] { settings.Summer, settings.Winter })
            {
                foreach (DateTime d in FirstWorkingDates(range, holidays, 3)) vacationWork.Add(d);
                foreach (DateTime d in LastWorkingDates(range, holidays, 5)) vacationWork.Add(d);
            }

        }
        internal static string DayCode(Settings settings,DateTime date,Dictionary<DateTime,string> holidays,HashSet<DateTime> vacationWork,bool seasonal){
            if(seasonal&&settings.NonConsent)return NonConsentPolicy.Code(settings,date,holidays);
            bool inVacation=settings.Summer.Contains(date)||settings.Winter.Contains(date),holiday=holidays.ContainsKey(date),weekday=date.DayOfWeek!=DayOfWeek.Saturday&&date.DayOfWeek!=DayOfWeek.Sunday;
            bool agreed=seasonal&&inVacation&&weekday&&!holiday&&settings.AgreedWork!=null&&settings.AgreedWork.Contains(date);
            bool active=!seasonal||!inVacation||vacationWork.Contains(date)||agreed;
            // 방학 중 토·일은 그 주에 유급 근무일이 있을 때만 유급이다. 공휴일만 있는 주에는 주휴가 새로 생기지 않는다. 다만 토·일 자체가 공휴일(설·추석 연휴 등)이면 동의자는 유급이다.
            bool weekendPaid=seasonal&&inVacation&&!active&&!weekday&&(holiday||WeekContainsPaidWork(date,settings,holidays,vacationWork));
            if(active||weekendPaid){if(holiday&&!weekendPaid)return "H";if(date.DayOfWeek==DayOfWeek.Saturday)return "S";if(date.DayOfWeek==DayOfWeek.Sunday)return "U";return seasonal&&vacationWork.Contains(date)?"A":"W";}
            return holiday&&weekday?"H":"N";
        }
        internal static CalendarDetail Analyze(Settings settings){
            Dictionary<DateTime,string> holidays;HashSet<DateTime> vacationWork;Prepare(settings,out holidays,out vacationWork);
            DateTime start=new DateTime(settings.Year,3,1),end=new DateTime(settings.Year+1,3,1);int dayCount=(end-start).Days;
            CalendarDetail detail=new CalendarDetail{Start=start,Codes=new string[dayCount],Holidays=holidays,Official=HolidayCalculator.GetFiscalHolidays(settings.Year),AutoWork=vacationWork,Agreed=new HashSet<DateTime>(),Discretionary=new HashSet<DateTime>(),Extra=new HashSet<DateTime>()};
            int regularWork=0,regularPaid=0,seasonalWork=0,seasonalPaid=0;
            for(int i=0;i<dayCount;i++){
                DateTime date=start.AddDays(i);
                string regular=DayCode(settings,date,holidays,vacationWork,false),seasonal=DayCode(settings,date,holidays,vacationWork,true);
                detail.Codes[i]=seasonal;
                if(regular!="N")regularPaid++;if(IsPrescribedWorkday(regular))regularWork++;
                if(seasonal!="N")seasonalPaid++;if(IsPrescribedWorkday(seasonal))seasonalWork++;
                if(settings.AgreedWork!=null&&settings.AgreedWork.Contains(date)&&(settings.Summer.Contains(date)||settings.Winter.Contains(date))&&date.DayOfWeek!=DayOfWeek.Saturday&&date.DayOfWeek!=DayOfWeek.Sunday&&!holidays.ContainsKey(date)&&!vacationWork.Contains(date))detail.Agreed.Add(date);
            }
            foreach(DateTime date in settings.Discretionary)detail.Discretionary.Add(date.Date);
            foreach(ExtraHoliday item in settings.Extras)detail.Extra.Add(item.Date.Date);
            int learning=Math.Max(0,4-CalendarPolicy.DiscretionaryCount(settings));
            detail.Result=new CalculationResult{RegularWorkDays=Math.Max(0,regularWork-learning),RegularPaidDays=regularPaid,SeasonalWorkDays=Math.Max(0,seasonalWork-learning),SeasonalPaidDays=seasonalPaid};
            return detail;
        }
        internal static CalculationResult Calculate(Settings settings){
            Dictionary<DateTime,string> holidays;HashSet<DateTime> vacationWork;Prepare(settings,out holidays,out vacationWork);
            int regularWork=0,regularPaid=0,seasonalWork=0,seasonalPaid=0;
            for(DateTime date=new DateTime(settings.Year,3,1);date<new DateTime(settings.Year+1,3,1);date=date.AddDays(1)){
                string regular=DayCode(settings,date,holidays,vacationWork,false),seasonal=DayCode(settings,date,holidays,vacationWork,true);
                if(regular!="N")regularPaid++;if(IsPrescribedWorkday(regular))regularWork++;
                if(seasonal!="N")seasonalPaid++;if(IsPrescribedWorkday(seasonal))seasonalWork++;
            }
            int count=CalendarPolicy.DiscretionaryCount(settings);int learning=Math.Max(0,4-count);
            return new CalculationResult{RegularWorkDays=Math.Max(0,regularWork-learning),RegularPaidDays=regularPaid,SeasonalWorkDays=Math.Max(0,seasonalWork-learning),SeasonalPaidDays=seasonalPaid};
        }

        private static SheetTotals FillSheet(ExcelWorksheet sheet, Settings settings, Dictionary<DateTime, string> holidays, HashSet<DateTime> vacationWork, bool seasonal)
        {
            sheet.Cells[1, 1].Value = settings.Year + "학년도 교육공무직(" + (seasonal ? settings.NonConsent?"320일 비동의자":"방학중 비근무자" : "상시근로자") + ") 근무일수 산정";
            int totalWork = 0;
            int totalPaid = 0;
            for (int index = 0; index < 12; index++)
            {
                int month = index < 10 ? index + 3 : index - 9;
                int calendarYear = month >= 3 ? settings.Year : settings.Year + 1;
                int side = index < 6 ? 0 : 1;
                int row = BlockRows[index % 6];
                int calendarColumn = side == 0 ? 1 : 14;
                int summaryColumn = side == 0 ? 8 : 21;
                int noteColumn = side == 0 ? 13 : 26;
                MonthTotals totals = FillMonth(sheet, calendarYear, month, row, calendarColumn, summaryColumn, noteColumn, settings, holidays, vacationWork, seasonal);
                totalWork += totals.WorkDays;
                totalPaid += totals.PaidDays;
            }

            int discretionCount = CalendarPolicy.DiscretionaryCount(settings);
            int learning = Math.Max(0, 4 - discretionCount);
            sheet.Cells[55, 6].Value = discretionCount;
            sheet.Cells[56, 6].Value = learning;
            sheet.Cells[55, 12].Formula = "SUM(H5,H14,H22,H31,H39,H48,U5,U14,U22,U31,U39,U48)-F56";
            sheet.Cells[55, 21].Formula = "SUM(L5,L14,L22,L31,L39,L48,Y5,Y14,Y22,Y31,Y39,Y48)";
            sheet.Cells[57, 1, 61, 26].Clear();
            for (int i = sheet.Comments.Count - 1; i >= 0; i--) sheet.Comments.Remove(sheet.Comments[i]);
            for (int column = 27; column <= 40; column++) sheet.Column(column).Hidden = true;
            return new SheetTotals { WorkDays = Math.Max(0, totalWork - learning), PaidDays = totalPaid };
        }

        private static MonthTotals FillMonth(ExcelWorksheet sheet, int year, int month, int headerRow, int startColumn, int summaryColumn, int noteColumn, Settings settings, Dictionary<DateTime, string> holidays, HashSet<DateTime> vacationWork, bool seasonal)
        {
            sheet.Cells[headerRow, startColumn].Value = month <= 2 ? year + "." + month + "월" : month + "월";
            int gridStart = headerRow + 2;
            int helperStartColumn = startColumn == 1 ? 27 : 34;
            for (int r = gridStart; r < gridStart + 6; r++)
                for (int c = startColumn; c < startColumn + 7; c++)
                {
                    ResetCalendarCell(sheet.Cells[r, c]);
                    sheet.Cells[r, helperStartColumn + c - startColumn].Value = null;
                }
            for (int r = gridStart; r < gridStart + 6; r++) sheet.Cells[r, noteColumn].Value = null;

            int work = 0, sat = 0, sun = 0, paidHoliday = 0, paidDays = 0;
            List<Tuple<int, int, string>> notes = new List<Tuple<int, int, string>>();
            List<Tuple<int, int, string>> holidayNotes = new List<Tuple<int, int, string>>();
            int noteOrder = 0;
            List<int> vacationCleaningDays = new List<int>();
            DateTime first = new DateTime(year, month, 1);
            int offset = (int)first.DayOfWeek;
            int days = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= days; day++)
            {
                DateTime date = new DateTime(year, month, day);
                int position = offset + day - 1;
                ExcelRange cell = sheet.Cells[gridStart + position / 7, startColumn + position % 7];
                cell.Value = day;
                bool inVacation = settings.Summer.Contains(date) || settings.Winter.Contains(date);
                bool holiday = holidays.ContainsKey(date);
                bool weekday = date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
                bool agreedWork = seasonal && !settings.NonConsent && inVacation && weekday && !holiday && settings.AgreedWork != null && settings.AgreedWork.Contains(date);
                bool active = !seasonal || !inVacation || vacationWork.Contains(date) || agreedWork;
                bool addedVacationWork = seasonal && vacationWork.Contains(date);
                if (addedVacationWork) vacationCleaningDays.Add(day);
                string statusCode=DayCode(settings,date,holidays,vacationWork,seasonal);
                if(statusCode!="N")paidDays++;
                if(IsPrescribedWorkday(statusCode))work++;
                if(statusCode=="S")sat++;
                if(statusCode=="U")sun++;
                if(statusCode=="H")paidHoliday++;
                if(seasonal&&settings.NonConsent)StyleDateCell(cell,date,statusCode=="H",statusCode=="N",false,statusCode=="W",statusCode=="S"||statusCode=="U");
                else {
                    bool vacationWeekendPaid=seasonal&&inVacation&&!active&&(statusCode=="S"||statusCode=="U");
                    StyleDateCell(cell,date,holiday,seasonal&&inVacation&&!active,addedVacationWork,active&&weekday&&!holiday,vacationWeekendPaid);
                }
                sheet.Cells[gridStart + position / 7, helperStartColumn + position % 7].Value = statusCode;

                if (holiday) holidayNotes.Add(Tuple.Create(day, noteOrder++, holidays[date]+(seasonal&&settings.NonConsent&&inVacation&&!NonConsentPolicy.PaidHoliday(settings,date,holidays)?statusCode=="N"?"(무급)":"(주휴 유급)":"")));
                if (date == settings.Summer.Start || date == settings.Winter.Start) notes.Add(Tuple.Create(day, noteOrder++, day + " 방학 시작"));
                if (date == settings.Summer.End || date == settings.Winter.End) notes.Add(Tuple.Create(day, noteOrder++, day + " 방학 종료"));
            }

            // 같은 휴일명과 유급/무급 구분이 이어지는 날짜만 묶고, 시작 날짜의 비고 순서는 유지한다.
            for (int i = 0; i < holidayNotes.Count;)
            {
                var firstNote = holidayNotes[i];
                int lastDay = firstNote.Item1;
                int next = i + 1;
                while (next < holidayNotes.Count && holidayNotes[next].Item1 == lastDay + 1 && holidayNotes[next].Item3 == firstNote.Item3)
                {
                    lastDay = holidayNotes[next].Item1;
                    next++;
                }
                string dateLabel = firstNote.Item1 == lastDay
                    ? firstNote.Item1.ToString()
                    : firstNote.Item1 + "일~" + lastDay + "일";
                notes.Add(Tuple.Create(firstNote.Item1, firstNote.Item2, dateLabel + " " + firstNote.Item3));
                i = next;
            }

            if (vacationCleaningDays.Count > 0)
            {
                int firstCleaningDay = vacationCleaningDays.Min();
                int lastCleaningDay = vacationCleaningDays.Max();
                string cleaningRange = firstCleaningDay == lastCleaningDay
                    ? firstCleaningDay.ToString()
                    : firstCleaningDay + " - " + lastCleaningDay;
                notes.Add(Tuple.Create(firstCleaningDay, noteOrder++, cleaningRange + " 방학중청소일"));
            }

            notes = notes.OrderBy(x => x.Item1).ThenBy(x => x.Item2).ToList();

            string helperRange = ExcelCellBase.GetAddress(gridStart, helperStartColumn, gridStart + 5, helperStartColumn + 6);
            sheet.Cells[gridStart, summaryColumn].Formula = "COUNTIF(" + helperRange + ",\"W\")+COUNTIF(" + helperRange + ",\"A\")+COUNTIF(" + helperRange + ",\"L\")";
            sheet.Cells[gridStart, summaryColumn + 1].Formula = "COUNTIF(" + helperRange + ",\"S\")";
            sheet.Cells[gridStart, summaryColumn + 2].Formula = "COUNTIF(" + helperRange + ",\"U\")";
            sheet.Cells[gridStart, summaryColumn + 3].Formula = "COUNTIF(" + helperRange + ",\"H\")";
            sheet.Cells[gridStart, summaryColumn + 4].Formula = "SUM(" + ExcelCellBase.GetAddress(gridStart, summaryColumn) + ":" + ExcelCellBase.GetAddress(gridStart, summaryColumn + 3) + ")";
            for (int i = 0; i < Math.Min(6, notes.Count); i++) sheet.Cells[gridStart + i, noteColumn].Value = notes[i].Item3;
            if (notes.Count > 6) sheet.Cells[gridStart + 5, noteColumn].Value = "외 " + (notes.Count - 5) + "건";
            return new MonthTotals { WorkDays = work, PaidDays = paidDays };
        }

        private static void ResetCalendarCell(ExcelRange cell)
        {
            cell.Value = null;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.White);
            cell.Style.Font.Color.SetColor(Color.Black);
            cell.Style.Font.Bold = false;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        private static void StyleDateCell(ExcelRange cell, DateTime date, bool holiday, bool vacationOff, bool vacationWork, bool regularWork, bool vacationWeekendPaid)
        {
            Color fill = Color.White;
            if (regularWork) fill = RegularWorkFill;
            if (vacationOff && !holiday) fill = VacationFill;
            if (vacationWork) fill = VacationWorkFill;
            if (date.DayOfWeek == DayOfWeek.Saturday && (!vacationOff || vacationWeekendPaid)) fill = SaturdayFill;
            if (date.DayOfWeek == DayOfWeek.Sunday && (!vacationOff || vacationWeekendPaid)) fill = SundayFill;
            if (holiday) fill = HolidayFill;
            cell.Style.Font.Color.SetColor(Color.Black);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(fill);
            cell.Style.Fill.PatternColor.SetColor(fill);
            cell.Style.Font.Bold = holiday || vacationWork;
        }

        private static IEnumerable<DateTime> FirstWorkingDates(DateRange range, Dictionary<DateTime, string> holidays, int count)
        {
            int found = 0;
            for (DateTime date = range.Start; date <= range.End && found < count; date = date.AddDays(1))
                if (IsAvailableWeekday(date, holidays)) { found++; yield return date; }
        }

        private static IEnumerable<DateTime> LastWorkingDates(DateRange range, Dictionary<DateTime, string> holidays, int count)
        {
            List<DateTime> dates = new List<DateTime>();
            for (DateTime date = range.End; date >= range.Start && dates.Count < count; date = date.AddDays(-1))
                if (IsAvailableWeekday(date, holidays)) dates.Add(date);
            dates.Reverse();
            return dates;
        }

        private static bool IsAvailableWeekday(DateTime date, Dictionary<DateTime, string> holidays)
        {
            return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday && !holidays.ContainsKey(date);
        }

        private static bool WeekContainsPaidWork(DateTime date, Settings settings, Dictionary<DateTime, string> holidays, HashSet<DateTime> vacationWork)
        {
            int daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
            DateTime monday = date.AddDays(-daysFromMonday).Date;
            for (int i = 0; i < 7; i++)
            {
                DateTime candidate=monday.AddDays(i);if(candidate.DayOfWeek==DayOfWeek.Saturday||candidate.DayOfWeek==DayOfWeek.Sunday)continue;
                if(vacationWork.Contains(candidate))return true;
                bool inVacation=settings.Summer.Contains(candidate)||settings.Winter.Contains(candidate);
                if(!inVacation&&!holidays.ContainsKey(candidate))return true;
                if(inVacation&&!holidays.ContainsKey(candidate)&&settings.AgreedWork!=null&&settings.AgreedWork.Contains(candidate))return true;
            }
            return false;
        }

        private static string AppendName(Dictionary<DateTime, string> holidays, DateTime date, string name)
        {
            string old;
            return holidays.TryGetValue(date, out old) ? old + "·" + name : name;
        }
    }

    internal static class HolidayCalculator
    {
        public static Dictionary<DateTime, string> GetFiscalHolidays(int year)
        {
            if (!Holidays.Covers(year) || !Holidays.Covers(year + 1))
                throw new InvalidOperationException(year + "학년도 공휴일 자료가 부족합니다. " + year + "년과 " + (year+1) + "년 자료가 모두 필요합니다. 공휴일 확인 후 다시 생성해 주세요.");
            var start = new DateTime(year,3,1); var end = new DateTime(year+1,3,1);
            var result = Holidays.Current.years.SelectMany(y=>y.holidays)
                .Select(d=>new { Date=DateTime.ParseExact(d.date,"yyyy-MM-dd",CultureInfo.InvariantCulture),d.name })
                .Where(d=>d.Date>=start && d.Date<end).ToDictionary(d=>d.Date,d=>d.name);
            // 근로자의 날은 기존 근로일수 산정 기준을 별도로 유지한다.
            if(!result.ContainsKey(new DateTime(year,5,1))) result[new DateTime(year,5,1)]="근로자의 날";
            return result;
        }
    }

    internal sealed class Settings
    {
        public int Year;
        public bool NonConsent;
        public DateRange Summer;
        public DateRange Winter;
        public List<DateTime> Discretionary;
        public Dictionary<DateTime, string> DiscretionaryNames;
        public List<ExtraHoliday> Extras;
        public HashSet<DateTime> AgreedWork = new HashSet<DateTime>();
        public string OutputFolder;
    }

    internal sealed class ExtraHoliday { public DateTime Date; public string Name; }
    internal sealed class DateRange
    {
        public readonly DateTime Start; public readonly DateTime End; public readonly string Name;
        public DateRange(DateTime start, DateTime end, string name) { Start = start.Date; End = end.Date; Name = name; }
        public bool IsValid { get { return End >= Start; } }
        public bool Contains(DateTime date) { return date.Date >= Start && date.Date <= End; }
        public bool Overlaps(DateRange other) { return Start <= other.End && other.Start <= End; }
    }
    internal sealed class CalendarDetail
    {
        public DateTime Start;
        public string[] Codes;
        public Dictionary<DateTime, string> Holidays;
        public Dictionary<DateTime, string> Official;
        public HashSet<DateTime> AutoWork;
        public HashSet<DateTime> Agreed;
        public HashSet<DateTime> Discretionary;
        public HashSet<DateTime> Extra;
        public CalculationResult Result;
        public string CodeOn(DateTime date) { int index = (date.Date - Start).Days; return index >= 0 && index < Codes.Length ? Codes[index] : "N"; }
    }
    internal sealed class MonthTotals { public int WorkDays; public int PaidDays; }
    internal sealed class SheetTotals { public int WorkDays; public int PaidDays; }
    internal sealed class CalculationResult { public int RegularWorkDays; public int RegularPaidDays; public int SeasonalWorkDays; public int SeasonalPaidDays; }
}
