using System;
using System.IO;
using OrbitIsland;
var dir=Path.Combine(Path.GetTempPath(),"orbit-models-"+Guid.NewGuid());var store=new Store(dir);
void Check(bool v,string name){if(!v)throw new Exception(name);Console.WriteLine("PASS "+name);}
store.Data.Settings.Top=0;store.Data.Settings.TextScale=9;store.Data.Settings.IconSize=90;store.Normalize();Check(store.Data.Settings.Top==6&&store.Data.Settings.TextScale==1.3&&store.Data.Settings.IconSize==24,"Appearance bounds");
store.Data.Notes.Add(new Note{Body="Title\nBody"});store.Data.Shelf.Add("example.lnk");store.Data.Todos.Add(new TodoItem{Text="Task"});store.Data.SeenWindowsNotices.Add("Windows · 1");store.Save();var loaded=new Store(dir);Check(loaded.Data.Notes[0].Title=="Title"&&loaded.Data.Shelf.Count==1&&loaded.Data.Todos[0].Text=="Task","Content persistence");Check(loaded.Data.SeenWindowsNotices.Count==1,"Cleared notifications stay seen after restart");Check(loaded.Data.Settings.Modules.Contains("AI Chat"),"AI module migration");Check(Store.DayProgress(new DateTime(2026,1,1,14,30,0),7,22)==.5,"Day midpoint");Check(Store.DayProgress(new DateTime(2026,1,1,23,0,0),7,22)==1,"Bedtime clamp");loaded.Save();File.WriteAllText(loaded.FilePath,"broken");var recovered=new Store(dir);Check(recovered.Data.Notes.Count==1&&recovered.RecoveryWarning!=null,"Backup recovery");Directory.Delete(dir,true);
