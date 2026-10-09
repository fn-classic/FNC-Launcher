using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace FssLauncher {
 public sealed class FriendEntry {public string userId,displayName,activity,avatar;public bool online,isBot;}
 public sealed class FriendSnapshot {public List<FriendEntry> friends=new List<FriendEntry>(),incoming=new List<FriendEntry>(),outgoing=new List<FriendEntry>();public int onlineCount;public bool canInvite;}
 public sealed partial class LauncherForm {
  FriendsPanel friendsPanel;FriendSnapshot friends=new FriendSnapshot();readonly string socialInstance=Guid.NewGuid().ToString("N");
  string socialError;DateTime nextSocial=DateTime.MinValue;bool socialPending;
  async Task RefreshSocial(){
   if(preview||profile==null||account==null||State!=StudioState.Online||socialPending||DateTime.UtcNow<nextSocial)return;
   socialPending=true;int generation=authGeneration;var client=account;
   try {
    string activity=connection.GameProcessId==0?"Launcher":connection.GameFrontend?"In lobby":"In match";
    await client.RequestData("social/presence",new{instance=socialInstance,activity=activity});
    var data=await client.RequestData("social/friends",null);
    if(generation!=authGeneration)return;
    socialError=null;friends=new JavaScriptSerializer().ConvertToType<FriendSnapshot>(data);
    FriendsButton.Text=friends.onlineCount.ToString();
   }catch(AccountFailure error){if(generation==authGeneration){socialError=error.Message;friends=new FriendSnapshot();FriendsButton.Text="";}}
   finally{socialPending=false;nextSocial=DateTime.UtcNow.AddSeconds(10);}
  }
  async Task<FriendSnapshot> FriendsData(){while(socialPending)await Task.Delay(50);nextSocial=DateTime.MinValue;await RefreshSocial();if(socialError!=null)throw new InvalidOperationException(socialError);return friends;}
  async Task<List<FriendEntry>> SearchFriends(string term){if(account==null)throw new InvalidOperationException("Sign in first.");var data=await account.RequestData("social/search?q="+Uri.EscapeDataString(term),null);return new JavaScriptSerializer().ConvertToType<List<FriendEntry>>(data["users"]);}
  async Task FriendAction(string action,string id){if(account==null)throw new InvalidOperationException("Sign in first.");await account.RequestData(action=="invite"?"social/invite":"social/action",new{action=action,userId=id});nextSocial=DateTime.MinValue;}
  async Task LeaveSocial(){var client=account;if(client!=null)try{await client.RequestData("social/presence",new{instance=socialInstance,activity="Offline"});}catch(AccountFailure){}friends=new FriendSnapshot();FriendsButton.Text="0";}
  void ShowFriends(){if(!gate.Allowed)return;if(friendsPanel!=null&&!friendsPanel.IsDisposed){friendsPanel.Close();return;}var panel=new FriendsPanel(FriendsData,SearchFriends,FriendAction,profile.displayName);friendsPanel=panel;panel.FormClosed+=delegate{friendsPanel=null;panel.Dispose();};panel.Show(this);}
 }
}
