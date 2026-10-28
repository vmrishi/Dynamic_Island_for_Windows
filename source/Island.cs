using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
namespace OrbitIsland;
public class Program {
 [STAThread] public static void Main(string[] args){
  string dir=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OrbitDynamicIsland");var di=Array.IndexOf(args,"--data-dir");if(di>=0&&args.Length>di+1)dir=System.IO.Path.GetFullPath(args[di+1]);
  if(args.Contains("--self-test")){SelfTest.Run(dir);return;} if(args.Contains("--diagnostics")){SelfTest.Diagnostics(dir);return;}
  using var mutex=new Mutex(true,"Local\\OrbitIsland-v01-"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(dir)))[..16],out bool first);if(!first)return;
  var app=new Application{ShutdownMode=ShutdownMode.OnMainWindowClose};
  app.DispatcherUnhandledException+=(s,e)=>{try{Directory.CreateDirectory(dir);File.AppendAllText(System.IO.Path.Combine(dir,"errors.log"),DateTime.Now+" "+e.Exception+Environment.NewLine);MessageBox.Show(e.Exception.Message,"Orbit Island");}catch{}e.Handled=true;};
  app.Run(new Island(new Store(dir),args.Contains("--expanded")));
 }
}
public partial class Island:Window {
 readonly Store store; AppData D=>store.Data; Preferences S=>D.Settings;
 Border shell=new(); DockPanel layout=new(); StackPanel nav=new(); Grid body=new(); TextBlock status=new(); TextBlock clock=new(); Button bell=new();
 DispatcherTimer tick=new(){Interval=TimeSpan.FromSeconds(1)}; DispatcherTimer leave=new(){Interval=TimeSpan.FromSeconds(1.4)}; Forms.NotifyIcon tray=new();
 string tab="Home",todoFilter="Active",connectionTab="Wi-Fi",notesId=""; bool expanded,keepAwake,busyMedia; int seconds; DateTime lastTick=DateTime.UtcNow,lastWater=DateTime.Now,lastEye=DateTime.Now,lastMovement=DateTime.Now;
 DateTime selectedDate=DateTime.Today; string healthMode=""; DateTimeOffset? healthEnd; int healthLength;
 JsonElement? media,weather; string mediaError="",weatherError="",connectionError="",brightnessError=""; int? brightness;
 List<(string Name,int Signal)> networks=new(); List<(string Name,string State)> devices=new(); Dictionary<string,string> radios=new(); string wifiDetails="";
 (double Cpu,double Memory,double UsedGb,double TotalGb,double Rx,double Tx) metrics;
 List<double> cpuHistory=new(),memHistory=new(),rxHistory=new(); TextBlock? timerText,timerState,miniTimer,healthText; Window? settingsWindow;
 static readonly Dictionary<string,string> Icons=new(){["Home"]="\uE80F",["Dashboard"]="\uECA5",["Control"]="\uE9E9",["Shelf"]="\uE7B8",["Todo"]="\uE73A",["Notes"]="\uE70B",["Pomodoro"]="\uE916",["Health"]="\uEB51",["Weather"]="\uE706",["Screen Time"]="\uE917",["Day Progress"]="\uE9D9",["Connections"]="\uE702",["System"]="\uE9D9",["AI Chat"]="\uE99A",["Calendar"]="\uE787"};
 Brush Accent=>Brush(S.Accent,"#58B9FF"); Brush Fg=>Brush(S.Light?"#172033":"#F5F6FA"); Brush Muted=>Brush(S.Light?"#657084":"#9195A3"); Brush Surface=>Brush(S.Light?"#EFF2F7":"#101010"); Brush CardBorder=>Brush(S.Light?"#DFE4ED":"#222222");
 public static SolidColorBrush Brush(string color,string fallback="#58B9FF"){try{return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));}catch{return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback));}}
 public Island(Store s,bool open){store=s;Title="Orbit Island";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=true;FontFamily=new FontFamily("Segoe UI");FontSize=14*S.TextScale;Foreground=Fg;
  var resources=(ResourceDictionary)XamlReader.Parse("""
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
 <Style TargetType="Button"><Setter Property="Cursor" Value="Hand"/><Setter Property="Padding" Value="12,8"/><Setter Property="Margin" Value="0,0,6,0"/><Setter Property="BorderThickness" Value="0"/><Setter Property="FocusVisualStyle" Value="{x:Null}"/><Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button"><Border x:Name="bg" Background="{TemplateBinding Background}" CornerRadius="11" Padding="{TemplateBinding Padding}"><ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/></Border><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="bg" Property="Opacity" Value="0.76"/></Trigger><Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="bg" Property="BorderBrush" Value="#58B9FF"/><Setter TargetName="bg" Property="BorderThickness" Value="1"/></Trigger><Trigger Property="IsEnabled" Value="False"><Setter TargetName="bg" Property="Opacity" Value="0.45"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
 <Style TargetType="TextBox"><Setter Property="Padding" Value="12,9"/><Setter Property="BorderThickness" Value="1"/><Setter Property="CaretBrush" Value="#58B9FF"/><Setter Property="SelectionBrush" Value="#367AA8"/><Setter Property="Template"><Setter.Value><ControlTemplate TargetType="TextBox"><Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="10"><Grid><ScrollViewer x:Name="PART_ContentHost" Margin="{TemplateBinding Padding}"/><TextBlock x:Name="watermark" Text="{TemplateBinding Tag}" Foreground="#9195A3" Margin="{TemplateBinding Padding}" IsHitTestVisible="False" Visibility="Collapsed" TextWrapping="Wrap"/></Grid></Border><ControlTemplate.Triggers><Trigger Property="Text" Value=""><Setter TargetName="watermark" Property="Visibility" Value="Visible"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
 <Style TargetType="ToolTip"><Setter Property="Background" Value="#222222"/><Setter Property="Foreground" Value="White"/><Setter Property="Padding" Value="10,6"/></Style>
</ResourceDictionary>
""");Resources=resources;
  Content=shell;shell.Margin=new Thickness(16,4,16,18);shell.CornerRadius=new CornerRadius(38);shell.BorderThickness=new Thickness(1);shell.Effect=new System.Windows.Media.Effects.DropShadowEffect{BlurRadius=22,Opacity=.35,ShadowDepth=6};
  SourceInitialized+=(_,_)=>{var h=new WindowInteropHelper(this).Handle;HwndSource.FromHwnd(h)?.AddHook(Hook);if(!Native.RegisterHotKey(h,1,0x4005,0x4F))Notify("Shortcut unavailable","Alt+Shift+O is used by another application. Use the tray icon.",false);};
  tray.Icon=System.Drawing.SystemIcons.Application;tray.Text="Orbit Island · Alt+Shift+O";tray.Visible=true;tray.DoubleClick+=(_,_)=>Dispatcher.Invoke(()=>{Show();SetExpanded(!expanded);Activate();});
  var menu=new Forms.ContextMenuStrip();menu.Items.Add("Open island",null,(_,_)=>Dispatcher.Invoke(()=>{Show();SetExpanded(true);Activate();}));menu.Items.Add("Customize",null,(_,_)=>Dispatcher.Invoke(OpenSettings));menu.Items.Add("Quit",null,(_,_)=>Dispatcher.Invoke(Close));tray.ContextMenuStrip=menu;
  shell.MouseEnter+=(_,_)=>{leave.Stop();if(!expanded&&S.OpenOnHover)SetExpanded(true);};shell.MouseLeave+=(_,_)=>{if(S.AutoCollapse)leave.Start();};leave.Tick+=(_,_)=>{leave.Stop();if(!IsMouseOver&&settingsWindow==null)SetExpanded(false);};
  PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){SetExpanded(false);e.Handled=true;}if(e.Key==Key.OemComma&&(Keyboard.Modifiers&ModifierKeys.Control)!=0)OpenSettings();};
  Closed+=(_,_)=>{tick.Stop();leave.Stop();Native.UnregisterHotKey(new WindowInteropHelper(this).Handle,1);Native.SetThreadExecutionState(0x80000000);tray.Dispose();settingsWindow?.Close();Save();};
  tick.Tick+=Tick;tick.Start();SetExpanded(open);Loaded+=async(_,_)=>{if(store.RecoveryWarning!=null)Notify("Data recovered",store.RecoveryWarning);await RefreshMedia();await RefreshBrightness();if(S.Latitude!=null)await RefreshWeather();};
 }
 IntPtr Hook(IntPtr h,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x312&&w.ToInt32()==1){Show();SetExpanded(!expanded);Activate();handled=true;}return IntPtr.Zero;}
 void Save(){try{store.Save();}catch(Exception e){status.Text="Save failed: "+e.Message;}}
 void Place(){var screens=Forms.Screen.AllScreens;var scr=screens[Math.Clamp(S.Monitor,0,screens.Length-1)];var dpi=VisualTreeHelper.GetDpi(this);Width=expanded?Math.Min(S.Width,scr.WorkingArea.Width/dpi.DpiScaleX-24):220;Height=expanded?(tab is "Home" or "Dashboard" or "Pomodoro" or "System"?332:432):74;Left=scr.WorkingArea.Left/dpi.DpiScaleX+(scr.WorkingArea.Width/dpi.DpiScaleX-Width)/2;Top=scr.Bounds.Top/dpi.DpiScaleY+Math.Max(6,S.Top);}
 void SetExpanded(bool value){Title=S.Name;tray.Text=(S.Name+" · Alt+Shift+O")[..Math.Min(63,(S.Name+" · Alt+Shift+O").Length)];expanded=value;Topmost=S.AlwaysOnTop;Opacity=S.Opacity;Foreground=Fg;FontSize=14*S.TextScale;shell.Background=Brush(S.Light?"#FBFCFE":S.IslandColor,"#000000");shell.BorderBrush=Brushes.Transparent;Place();Build();}
 void Build(){timerText=null;timerState=null;healthText=null;miniTimer=null;layout=new DockPanel{Margin=new Thickness(18,14,18,12)};shell.Child=layout;
  if(!expanded){var mini=new Grid();mini.ColumnDefinitions.Add(new(){Width=GridLength.Auto});mini.ColumnDefinitions.Add(new());mini.ColumnDefinitions.Add(new(){Width=GridLength.Auto});mini.Children.Add(T("◉",20,Accent));clock=T(DateTime.Now.ToString("HH:mm"),16,Fg,true);clock.HorizontalAlignment=HorizontalAlignment.Center;Grid.SetColumn(clock,1);mini.Children.Add(clock);miniTimer=T(D.Timer.Running?FormatTime(TimerSeconds()):"⌄",14,Accent);Grid.SetColumn(miniTimer,2);mini.Children.Add(miniTimer);mini.MouseLeftButtonUp+=(_,_)=>SetExpanded(true);layout.Children.Add(mini);return;}
  var header=new Grid{Margin=new Thickness(0,0,0,15)};header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});header.ColumnDefinitions.Add(new());header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var logo=Btn("◉",()=>Go("Home"),false,"Home");logo.Foreground=Accent;logo.FontSize=22;logo.Padding=new Thickness(3,2,12,2);header.Children.Add(logo);
  nav=new StackPanel{Orientation=Orientation.Horizontal};foreach(var n in S.Modules.Where(x=>!S.Hidden.Contains(x))){var active=n==tab;var p=new StackPanel{Orientation=Orientation.Horizontal};p.Children.Add(new TextBlock{Text=Icons[n],FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=S.IconSize,VerticalAlignment=VerticalAlignment.Center,Foreground=active?Fg:Muted});if(active){var label=T(n,13,Fg,true);label.Margin=new Thickness(7,0,0,0);p.Children.Add(label);}var b=Btn("",()=>Go(n),active,n);b.Content=p;b.Padding=new Thickness(10,8,10,8);nav.Children.Add(b);}
  var ns=new ScrollViewer{Content=nav,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetColumn(ns,1);header.Children.Add(ns);
  var actions=Row();var collapse=Btn("⌃",()=>SetExpanded(false),false,"Collapse · Esc");actions.Children.Add(collapse);bell=Btn("\uE7F4",()=>Go("Notifications"),tab=="Notifications","Notifications");bell.FontFamily=new FontFamily("Segoe MDL2 Assets");if(D.Notices.Any(x=>!x.Read))bell.Foreground=Accent;actions.Children.Add(bell);var settings=Btn("\uE713",OpenSettings,false,"Customize · Ctrl+,");settings.FontFamily=new FontFamily("Segoe MDL2 Assets");actions.Children.Add(settings);Grid.SetColumn(actions,2);header.Children.Add(actions);DockPanel.SetDock(header,Dock.Top);layout.Children.Add(header);
  var footer=new DockPanel{Margin=new Thickness(0,6,0,0),Visibility=Visibility.Collapsed};clock=T(DateTime.Now.ToString("ddd, dd MMM · HH:mm"),11,Muted);DockPanel.SetDock(clock,Dock.Right);footer.Children.Add(clock);status=T(S.Name+"  /  "+tab,11,Muted);footer.Children.Add(status);DockPanel.SetDock(footer,Dock.Bottom);layout.Children.Add(footer);
  body=new Grid();layout.Children.Add(body);RenderTab();
 }
 async void Go(string name){tab=name;expanded=true;Place();Build();if(name is "Connections" or "Control")await RefreshConnections();}
 void RenderTab(){body.Children.Clear();try{switch(tab){case "Home":Home();break;case "Dashboard":Dashboard();break;case "Control":Control();break;case "Shelf":Shelf();break;case "Todo":Todos();break;case "Notes":Notes();break;case "Pomodoro":Pomodoro();break;case "Health":Health();break;case "Weather":Weather();break;case "Screen Time":ScreenTime();break;case "Day Progress":DayProgress();break;case "Connections":Connections();break;case "System":SystemPage();break;case "AI Chat":AiChat();break;case "Calendar":CalendarPage();break;case "Notifications":Notifications();break;}}catch(Exception e){Empty("This view is unavailable",e.Message);}}
 TextBlock T(string text,double size=14,Brush? color=null,bool bold=false)=>new(){Text=text,FontSize=size*S.TextScale,Foreground=color??Fg,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};
 Button Btn(string text,Action action,bool active=false,string? tip=null){var b=new Button{Content=text,Background=active?Brush(S.Light?"#DDE9F5":"#303030"):Brush(S.Light?"#E6EBF2":"#191919"),Foreground=Fg,ToolTip=tip??text};AutomationProperties.SetName(b,tip??text);b.Click+=(_,_)=>{try{action();}catch(Exception e){Notify("Action unavailable",e.Message);}};return b;}
 Button Primary(string text,Action action){var b=Btn(text,action);b.Background=Accent;b.Foreground=Brush("#08121F");b.FontWeight=FontWeights.SemiBold;return b;}
 TextBox Input(string value="",string hint="",bool multiline=false){var b=new TextBox{Text=value,Tag=hint,Background=Surface,Foreground=Fg,BorderBrush=CardBorder,ToolTip=hint,AcceptsReturn=multiline,TextWrapping=multiline?TextWrapping.Wrap:TextWrapping.NoWrap,VerticalContentAlignment=VerticalAlignment.Center};AutomationProperties.SetName(b,hint);return b;}
 StackPanel Col(params UIElement[] children){var p=new StackPanel();foreach(var c in children)p.Children.Add(c);return p;}
 StackPanel Row(params UIElement[] children){var p=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};foreach(var c in children)p.Children.Add(c);return p;}
 Border Card(UIElement child,int padding=18)=>new(){Background=Surface,BorderBrush=CardBorder,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(19),Padding=new Thickness(padding),Child=child,Margin=new Thickness(0,0,12,0)};
 Grid Columns(params double[] widths){var g=new Grid();foreach(var w in widths)g.ColumnDefinitions.Add(new(){Width=new GridLength(w,GridUnitType.Star)});return g;}
 void Put(Grid grid,UIElement child,int col,int row=0){Grid.SetColumn(child,col);Grid.SetRow(child,row);grid.Children.Add(child);}
 ScrollViewer Scroll(UIElement child)=>new(){Content=child,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
 void Gap(StackPanel p,double h=12)=>p.Children.Add(new Border{Height=h});
 void Empty(string title,string detail,UIElement? action=null){var p=Col(T(title,25,Fg,true),T(detail,14,Muted));p.HorizontalAlignment=HorizontalAlignment.Center;p.VerticalAlignment=VerticalAlignment.Center;Gap(p,20);if(action!=null)p.Children.Add(action);body.Children.Add(p);}
 void Notify(string title,string detail,bool balloon=true){D.Notices.Insert(0,new Notice{Title=title,Detail=detail});D.Notices=D.Notices.Take(200).ToList();Save();if(balloon&&S.Reminders){tray.BalloonTipTitle=title;tray.BalloonTipText=detail;tray.ShowBalloonTip(5000);}if(expanded){status.Text=title+" · "+detail;bell.Foreground=Accent;}}
 void TryOpen(string target){try{Native.Open(target);}catch(Exception e){Notify("Could not open",e.Message);}}
 async void Tick(object? sender,EventArgs e){var now=DateTime.UtcNow;var elapsed=Math.Clamp((now-lastTick).TotalSeconds,0,3);lastTick=now;seconds++;clock.Text=expanded?DateTime.Now.ToString("ddd, dd MMM · HH:mm"):DateTime.Now.ToString("HH:mm");
  if(keepAwake)Native.SetThreadExecutionState(0x80000001);
  if(S.TrackApps){var app=Native.ForegroundApp();if(app!=null){var day=D.Today;day.Apps.TryGetValue(app,out var old);day.Apps[app]=old+elapsed;}}
  if(D.Timer.Running&&D.Timer.Mode!="Stopwatch"&&TimerSeconds()<=0){D.Timer.Running=false;D.Timer.Remaining=0;if(D.Timer.Mode=="Focus"){var completed=D.Day(D.Timer.Deadline?.LocalDateTime.Date??DateTime.Today);completed.FocusSessions++;completed.FocusSeconds+=D.Timer.SessionDuration;}Notify(D.Timer.Mode+" complete",D.Timer.Mode=="Focus"?"Take a break. Your session has been saved.":"Your break has finished.");if(tab=="Pomodoro"&&expanded)RenderTab();}
  if(timerText!=null)timerText.Text=FormatTime(TimerSeconds());if(timerState!=null)timerState.Text=D.Timer.Running?"In progress · you have this":"Ready when you are";if(miniTimer!=null)miniTimer.Text=D.Timer.Running?FormatTime(TimerSeconds()):"⌄";
  if(healthEnd!=null){var remaining=(int)Math.Ceiling((healthEnd.Value-DateTimeOffset.Now).TotalSeconds);if(remaining<=0){if(healthMode=="Eyes"){D.Today.Eyes++;lastEye=DateTime.Now;}if(healthMode=="Movement"){D.Today.Movement++;lastMovement=DateTime.Now;}if(healthMode=="Breathing")D.Today.Breathing++;healthEnd=null;Notify("Break complete","A little reset goes a long way.");if(tab=="Health"&&expanded)RenderTab();}else if(healthText!=null)healthText.Text=healthMode=="Breathing"?BreathCue(healthLength-remaining)+" · "+remaining+"s":healthMode+" · "+remaining+"s";}
  foreach(var t in D.Todos.Where(t=>!t.Done&&!t.Trashed&&!t.Alerted&&t.Due!=null&&t.Due<=DateTime.Now).ToList()){t.Alerted=true;Notify("Task due",t.Text);}
  foreach(var ev in D.Events.Where(x=>!x.Alerted&&x.Start>DateTime.Now.AddMinutes(-5)&&x.Start<=DateTime.Now.AddMinutes(5)).ToList()){ev.Alerted=true;Notify("Coming up",ev.Title+" · "+ev.Start.ToString("HH:mm"));}
  if(S.Reminders){if(S.WaterReminder>0&&(DateTime.Now-lastWater).TotalMinutes>=S.WaterReminder){lastWater=DateTime.Now;Notify("Water break","Time for a glass of water.");}if(S.EyeReminder>0&&(DateTime.Now-lastEye).TotalMinutes>=S.EyeReminder){lastEye=DateTime.Now;Notify("Rest your eyes","Look at something 20 feet away for 20 seconds.");}if(S.MovementReminder>0&&(DateTime.Now-lastMovement).TotalMinutes>=S.MovementReminder){lastMovement=DateTime.Now;Notify("Time to move","Stand up and stretch for a moment.");}}
  if(seconds%2==0){metrics=Native.Metrics();AddHistory(cpuHistory,metrics.Cpu);AddHistory(memHistory,metrics.Memory);AddHistory(rxHistory,metrics.Rx);if(tab=="System"&&expanded)RenderTab();}
  if(seconds%10==0&&((expanded&&(tab=="Home"||tab=="Dashboard"))||!expanded))await RefreshMedia();
  if(seconds%30==0){if(expanded&&tab is "Connections" or "Control")await RefreshConnections();if(S.SystemNotifications)await RefreshSystemNotifications();Save();if(expanded&&tab=="Screen Time")RenderTab();}
  if(seconds%1800==0&&S.Latitude!=null)await RefreshWeather();
 }
 static void AddHistory(List<double> list,double value){list.Add(value);if(list.Count>50)list.RemoveAt(0);}
 double TimerSeconds(){var t=D.Timer;if(t.Mode=="Stopwatch")return t.StopwatchSeconds+(t.Running&&t.StopwatchStart!=null?(DateTimeOffset.Now-t.StopwatchStart.Value).TotalSeconds:0);return t.Running&&t.Deadline!=null?Math.Max(0,(t.Deadline.Value-DateTimeOffset.Now).TotalSeconds):t.Remaining;}
 static string FormatTime(double secs){var t=TimeSpan.FromSeconds(Math.Max(0,Math.Ceiling(secs)));return t.TotalHours>=1?$"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}":$"{(int)t.TotalMinutes:00}:{t.Seconds:00}";}
 static string BreathCue(int seconds)=>seconds%14<4?"Breathe in":seconds%14<8?"Hold gently":"Breathe out";
 async Task RefreshMedia(){if(busyMedia)return;busyMedia=true;try{media=await Native.Broker("media");mediaError="";}catch(Exception ex){media=null;mediaError=ex.Message;}finally{busyMedia=false;}if(expanded&&tab=="Home")RenderTab();}
 async Task RefreshBrightness(){try{var b=await Native.Broker("brightness");brightness=b.GetProperty("value").GetInt32();}catch(Exception e){brightness=null;brightnessError=e.Message;}if(expanded&&tab=="Control")RenderTab();}
 string MediaString(string key,string fallback=""){try{return media?.GetProperty(key).GetString()??fallback;}catch{return fallback;}}
 double MediaNumber(string key){try{return media?.GetProperty(key).GetDouble()??0;}catch{return 0;}}
 bool HasMedia(){try{return media?.GetProperty("available").GetBoolean()??false;}catch{return false;}}
 void Home(){var g=Columns(1.8,1);body.Children.Add(g);var mp=Col(T("NOW PLAYING",11,Muted,true));Gap(mp,22);var song=Row(Card(T("♫",55,Accent),12),Col(T(HasMedia()?MediaString("title","Untitled track"):"Nothing playing",24,Fg,true),T(HasMedia()?MediaString("artist"):"Play something in Spotify, your browser, or another media app.",13,Muted)));mp.Children.Add(song);Gap(mp,25);var duration=MediaNumber("duration");mp.Children.Add(Progress(duration>0?MediaNumber("position")/duration:0,Accent,5));Gap(mp,8);mp.Children.Add(T(FormatTime(MediaNumber("position"))+"  /  "+FormatTime(duration),11,Muted));Gap(mp,16);var controls=Row(Btn("⏮",()=>Native.MediaKey(0xB1),false,"Previous track"),Primary(MediaString("status")=="Playing"?"Ⅱ":"▶",()=>Native.MediaKey(0xB3)),Btn("⏭",()=>Native.MediaKey(0xB0),false,"Next track"),Btn("↻",async()=>await RefreshMedia(),false,"Refresh media"));mp.Children.Add(controls);if(mediaError!=""){Gap(mp,8);mp.Children.Add(T("Windows media unavailable · media keys still work.",11,Muted));}Put(g,Card(mp),0);
  var cp=Col(T(DateTime.Today.ToString("dddd, dd MMMM"),19,Fg,true),T("YOUR DAY, AT A GLANCE",10,Muted,true));Gap(cp,20);foreach(var ev in D.Events.Where(x=>x.Start.Date==DateTime.Today).OrderBy(x=>x.Start).Take(3)){cp.Children.Add(T(ev.Start.ToString("HH:mm")+"  "+ev.Title,14));Gap(cp,9);}if(!D.Events.Any(x=>x.Start.Date==DateTime.Today))cp.Children.Add(T("A little room in your day.\nNo events scheduled.",14,Muted));Gap(cp,20);cp.Children.Add(Row(Btn("+ Event",()=>Go("Calendar")),Btn("Task view",()=>TryOpen("explorer.exe shell:::{3080F90E-D7AD-11D9-BD98-0000947B0257}"))));Gap(cp,16);cp.Children.Add(T($"{D.Todos.Count(x=>!x.Done&&!x.Trashed)} open tasks   ·   {D.Today.FocusSessions} focus sessions",12,Muted));Put(g,Card(cp),1);
 }
 FrameworkElement Progress(double value,Brush color,double height=7){var g=new Grid{Height=height,ClipToBounds=true,Margin=new Thickness(0,5,0,5)};g.Children.Add(new Border{Background=CardBorder,CornerRadius=new CornerRadius(height)});var fill=new Border{Background=color,CornerRadius=new CornerRadius(height),HorizontalAlignment=HorizontalAlignment.Left};g.Children.Add(fill);g.SizeChanged+=(_,_)=>fill.Width=Math.Max(0,g.ActualWidth*Math.Clamp(value,0,1));return g;}
 void Dashboard(){var g=Columns(1,1,1,1);body.Children.Add(g);var q=Col(T("QUICK TOGGLES",11,Muted,true));Gap(q,15);var grid=new UniformGrid{Columns=2};foreach(var (label,act) in QuickActions()){var b=Btn(label,act);b.Margin=new Thickness(0,0,7,8);b.MinHeight=42;grid.Children.Add(b);}q.Children.Add(grid);Put(g,Card(q),0);
  var wp=Col(T("OUTSIDE",11,Muted,true));Gap(wp,20);if(weather!=null){var cur=weather.Value.GetProperty("current");wp.Children.Add(T(cur.GetProperty("temperature_2m").GetDouble().ToString("0")+"°",45,Fg,true));wp.Children.Add(T(WeatherLabel(cur.GetProperty("weather_code").GetInt32()),16));wp.Children.Add(T(S.City,12,Muted));}else{wp.Children.Add(T("☁",48,Accent));wp.Children.Add(T("Your local forecast",18,Fg,true));Gap(wp);wp.Children.Add(Btn("Choose a city",()=>Go("Weather")));}Put(g,Card(wp),1);
  var apps=Col(T("QUICK ACCESS",11,Muted,true));Gap(apps,18);foreach(var pair in new[]{("Files","explorer.exe"),("Calculator","calc.exe"),("Notepad","notepad.exe"),("Windows Settings","ms-settings:")}){apps.Children.Add(Btn(pair.Item1,()=>TryOpen(pair.Item2)));Gap(apps,5);}Put(g,Card(apps),2);
  var dp=Col(T("DAY PROGRESS",11,Muted,true));Gap(dp,20);double value=Store.DayProgress(DateTime.Now,S.WakeHour,S.BedHour);dp.Children.Add(T(value.ToString("P0"),44,Fg,true));dp.Children.Add(Progress(value,Accent));Gap(dp,10);dp.Children.Add(T(value>=1?"Time to wind down 🌙":"Make room for what matters.",13,Muted));Gap(dp,20);dp.Children.Add(Btn("View your day →",()=>Go("Day Progress")));Put(g,Card(dp),3);
 }
 IEnumerable<(string,Action)> QuickActions(){yield return("Wi-Fi · "+ConnectedWifi(),()=>{connectionTab="Wi-Fi";Go("Connections");});yield return("Bluetooth · "+BluetoothSummary(),()=>{connectionTab="Bluetooth";Go("Connections");});yield return(keepAwake?"Awake · on":"Keep awake",()=>{keepAwake=!keepAwake;Native.SetThreadExecutionState(keepAwake?0x80000001:0x80000000);RenderTab();});yield return("Theme",()=>{Native.SetWindowsDark(!Native.WindowsDark());status.Text="Windows theme changed";});yield return("Microphone",()=>{Native.ToggleMute(1);status.Text="Microphone "+(Native.Audio(1).Muted?"muted":"unmuted");});yield return("Speaker",()=>{Native.ToggleMute(0);status.Text="Speaker "+(Native.Audio().Muted?"muted":"unmuted");});yield return("Screenshot",()=>TryOpen("ms-screenclip:"));yield return("Lock",()=>{Save();Native.LockWorkStation();});}
 void Control(){var p=Col();body.Children.Add(p);var tiles=new UniformGrid{Columns=4};foreach(var (label,act) in QuickActions()){var b=Btn(label,act);b.Height=66;b.Margin=new Thickness(0,0,10,10);tiles.Children.Add(b);}p.Children.Add(tiles);Gap(p,5);var sliders=Col();
  try{var audio=Native.Audio();sliders.Children.Add(SliderRow("Volume"+(audio.Muted?" · muted":""),audio.Volume*100,v=>{Native.SetVolume((float)(v/100));}));}catch{sliders.Children.Add(T("No default speaker is available.",13,Muted));}
  Gap(sliders,8);if(brightness!=null)sliders.Children.Add(SliderRow("Brightness",brightness.Value,async v=>{try{await Native.Broker("setbrightness",(int)v);brightness=(int)v;}catch(Exception e){Notify("Brightness unavailable",e.Message);}}));else sliders.Children.Add(Row(T("Brightness · not exposed by this display",12,Muted),Btn("Display settings",()=>TryOpen("ms-settings:display"))));Gap(sliders,10);sliders.Children.Add(Row(Btn("Sound output & input",()=>TryOpen("ms-settings:sound")),Btn("Eye break",()=>{Go("Health");StartHealth("Eyes",20);}),Btn("Refresh",async()=>await RefreshBrightness())));p.Children.Add(Card(sliders,14));}
 FrameworkElement SliderRow(string label,double value,Action<double> change){var g=Columns(1,4,.4);Put(g,T(label,13,Muted),0);var slider=new Slider{Minimum=0,Maximum=100,Value=value,VerticalAlignment=VerticalAlignment.Center,IsSnapToTickEnabled=true,TickFrequency=1,Foreground=Accent};AutomationProperties.SetName(slider,label);var val=T(((int)value).ToString(),13,Muted);slider.ValueChanged+=(_,_)=>val.Text=((int)slider.Value).ToString();slider.PreviewMouseLeftButtonUp+=(_,_)=>change(slider.Value);slider.PreviewKeyUp+=(_,_)=>change(slider.Value);Put(g,slider,1);Put(g,val,2);return g;}
}









