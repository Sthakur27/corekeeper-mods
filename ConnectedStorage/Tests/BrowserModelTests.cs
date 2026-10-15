using System;
using ConnectedStorage.Browser;
public static class BrowserModelTests
{
    static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    public static void Main()
    {
        var growth=new StorageExpansionTracker<int>();
        Require(!growth.Observe(new[]{2,3}),"First visit must not sort");
        Require(!growth.Observe(new[]{3,2}),"Reopening from another node must not sort");
        Require(growth.Observe(new[]{1,2,3}),"New node before existing slots must trigger sort");
        Require(!growth.Observe(new[]{1,2,3}),"Expansion must sort only once");
        Require(!growth.Observe(new[]{2,3}),"Removal must not sort");
        Require(growth.Observe(new[]{1,2,3}),"Reconnection must trigger sort");
        Require(!growth.Observe(new[]{8,9}),"Unrelated first visit must not sort");
        Require(growth.Observe(new[]{1,2,3,8,9}),"Joining networks must trigger sort");
        var m=new StorageBrowserModel<int>(2,1);
        m.SetEntries(new[]{
            new StorageBrowserModel<int>.Entry {Address=91,Name="Copper bar",Amount=4},
            new StorageBrowserModel<int>.Entry {Address=7,Name="Copper bar",Amount=2},
            new StorageBrowserModel<int>.Entry {Address=53,Name="Égg-shell",Amount=9},
            new StorageBrowserModel<int>.Entry {Address=100,Name="",Empty=true}
        });
        m.Configure("",BrowserSort.Amount,true,BrowserLayout.Grid);
        Require(m.Entries[0].Address==53 && m.Entries[3].Address==100,"Descending order or empty targets");
        m.ScrollToRow(100);
        Require(m.TryGetVisible(0,out var e) && e.Address==7,"Scroll must retain physical address");
        m.Configure("EGG SHELL",BrowserSort.Name,false,BrowserLayout.List);
        Require(m.Entries.Count==1 && m.Entries[0].Address==53 && m.FirstRow==0,"Search normalization/reset");
        m.Configure("Copper",BrowserSort.Name,true,BrowserLayout.Grid);
        Require(m.Entries.Count==2 && m.Entries[0].Address==91 && m.Entries[1].Address==7,"Duplicate stacks must stay distinct and stable");
        m.Configure("nothing",BrowserSort.Name,false,BrowserLayout.List);
        m.ScrollToRow(-5);
        Require(m.FirstRow==0 && !m.TryGetVisible(0,out _),"Empty search bounds");
        var chest=new StorageBrowserModel<int>(8,8);
        var slots=new System.Collections.Generic.List<StorageBrowserModel<int>.Entry>();
        for(int i=0;i<20;i++)slots.Add(new StorageBrowserModel<int>.Entry {Address=i,Empty=true});
        chest.Configure("",BrowserSort.Physical,false,BrowserLayout.Grid);
        chest.SetEntries(slots);
        Require(chest.TryGetVisible(19,out _) && !chest.TryGetVisible(20,out _),"20-slot chest must not draw row padding");
        slots[17]=new StorageBrowserModel<int>.Entry {Address=17,Name="Dirt",Amount=46};
        chest.SetEntries(slots);
        Require(chest.TryGetVisible(17,out var dirt) && dirt.Amount==46 && dirt.Address==17,"Deposit must stay in its physical slot");
        chest.Configure("Dirt",BrowserSort.Physical,false,BrowserLayout.Grid);
        Require(chest.TryGetVisible(0,out dirt) && dirt.Address==17,"Filtered slot must retain its target");
        chest.Configure("",BrowserSort.Physical,false,BrowserLayout.Grid);
        Require(chest.TryGetVisible(17,out dirt) && dirt.Address==17,"Clearing filter must restore physical layout");
        // Same physical network, reversed local entity IDs and enumeration order.
        var client=new[]{(id:1,x:10,z:4,capacity:20),(id:2,x:11,z:4,capacity:40)};
        var server=new[]{(id:1,x:11,z:4,capacity:40),(id:9,x:10,z:4,capacity:20)};
        Array.Sort(client,(a,b)=>StorageNodeOrder.Compare(a.x,a.z,b.x,b.z));
        Array.Sort(server,(a,b)=>StorageNodeOrder.Compare(a.x,a.z,b.x,b.z));
        int visible=0,occupied=32;
        for(int n=0;n<client.Length;n++) {
            Require(client[n].x==server[n].x && client[n].z==server[n].z,"Client/server node order must ignore entity IDs");
            for(int slot=0;slot<client[n].capacity;slot++,visible++) {
                int serverOffset=n==0?slot:server[0].capacity+slot;
                Require((serverOffset<occupied)==(visible<occupied),"Sorted 32/60 must occupy first 32 visible slots");
            }
        }
        Require(StorageNodeOrder.Compare(-4,2,3,2)<0 && StorageNodeOrder.Compare(3,-2,-4,2)<0,"Coordinate ordering across negative tiles");
        Console.WriteLine("Browser model: address identity, duplicate stacks, sorting, search, scrolling PASS");
    }
}
