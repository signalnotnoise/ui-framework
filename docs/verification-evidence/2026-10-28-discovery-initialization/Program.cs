using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using UI_Framework.Wpf;
using static UI_Framework.UI;
internal static class Program
{
 [STAThread] static void Main()
 {
  using var host=new ViewHost(()=>VirtualList(Enumerable.Range(0,1000).Select(i=>Button($"Open {i}",()=>{}).Id($"row-{i}").AccessibilityLabel($"Record {i}")),200));
  var window=new Window { Content=host, Width=500, Height=300,ShowInTaskbar=false};
  try
  {
   window.Show();window.UpdateLayout();
   var rootPeer=UIElementAutomationPeer.CreatePeerForElement(window)!;Initialize(rootPeer);
   var list=Find(host).First();
   var peer=(ItemsControlAutomationPeer)UIElementAutomationPeer.CreatePeerForElement(list)!;
   var itemPeer=(AutomationPeer)typeof(ItemsControlAutomationPeer).GetMethod("FindOrCreateItemAutomationPeer",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(peer,new[]{list.Items[999]})!;
   Console.WriteLine($"Logical count={list.Items.Count}; last name={itemPeer.GetName()}; parent={itemPeer.GetParent()?.GetType().Name}");
   Console.WriteLine($"Handles before: root={Handle(rootPeer)} list={Handle(peer)}");
   Console.WriteLine("Before native query: "+(((IItemContainerProvider)peer).FindItemByProperty(null!,AutomationElementIdentifiers.NameProperty.Id,"Record 999") is not null));
   var handle=new WindowInteropHelper(window).Handle;
   var frame=new DispatcherFrame(); var dispatcher=window.Dispatcher;
   var timer=new DispatcherTimer(TimeSpan.FromSeconds(20),DispatcherPriority.Send,(_,_)=>frame.Continue=false,dispatcher);timer.Start();
   var thread=new Thread(()=>
   {
    try { var element=AutomationElement.FromHandle(handle); Console.WriteLine("Native root query: "+element.Current.ClassName); Console.WriteLine("Native descendant query: "+(element.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Record 0")) is not null)); }
    catch(Exception error){Console.WriteLine(error);}
    finally {dispatcher.BeginInvoke(new Action(()=>frame.Continue=false));}
   }) {IsBackground=true}; thread.SetApartmentState(ApartmentState.MTA);thread.Start();
   Dispatcher.PushFrame(frame);timer.Stop();
   Console.WriteLine($"Handles after native query: root={Handle(rootPeer)} list={Handle(peer)}");
   Reset(rootPeer);
   Console.WriteLine($"Handles after reset: root={Handle(rootPeer)} list={Handle(peer)}");
   var target=((IItemContainerProvider)peer).FindItemByProperty(null!,AutomationElementIdentifiers.NameProperty.Id,"Record 999");
   Console.WriteLine("After native query: "+(target is not null));
   Console.WriteLine("Virtualized pattern: "+(target?.GetPatternProvider(VirtualizedItemPatternIdentifiers.Pattern.Id) is IVirtualizedItemProvider));
  }
  finally {window.Close();}
 }
 static object? Handle(AutomationPeer peer)=>typeof(AutomationPeer).GetProperty("Hwnd",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(peer);
 static void Reset(AutomationPeer peer){peer.ResetChildrenCache();foreach(var child in peer.GetChildren()??[])Reset(child);}
 static void Initialize(AutomationPeer peer){foreach(var child in peer.GetChildren()??[])Initialize(child);}
 static IEnumerable<ItemsControl> Find(DependencyObject parent)
 {
  if(parent is ItemsControl items)yield return items;
  for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)foreach(var item in Find(VisualTreeHelper.GetChild(parent,i)))yield return item;
 }
}
