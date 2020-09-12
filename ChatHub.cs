using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using SignalRChat.Models;

namespace SignalRChat
{
    public class ChatHub :Hub
    {

        static List<Users> ConnectedUsers = new List<Users>();
        static List<Messages> CurrentMessage = new List<Messages>();
        //static Boolean isStart=false;
             
        ConnClass ConnC = new ConnClass();
        SignalRdbEntities db = new SignalRdbEntities();


        public void Connect(string userName,string LoggedUserID)

        {
            //ConnectedUsID
            var id = Context.ConnectionId;
            var ConnectedUserID = Clients.Client(userName);
            string UserImg = GetUserImage(userName);
            string logintime = DateTime.Now.ToString();
            //IF User is not connected already
            if (ConnectedUsers.Count(x => x.ConnectionId == LoggedUserID) == 0)
            {
                //if (!isStart)
                //{
                //    isStart = true;
                    var result = from M in db.Messages
                                 select new
                                 {
                                     Name = M.tbl_Users.UserName,
                                     Messege = M.Message1,
                                     Time = M.Time,
                                     img = M.tbl_Users.Photo
                                 };
                    foreach (var item in result.ToList())
                    {
                        CurrentMessage.Add(new Messages { UserName = item.Name, Message = item.Messege, Time = item.Time, UserImage = item.img });
                    }

                ConnectedUsers.Add(new Users { ConnectionId = LoggedUserID, UserName = userName, UserImage = UserImg, LoginTime = logintime });           
               
                // send to all except caller client
                Clients.AllExcept(id).onNewUserConnected(id, userName, UserImg, logintime);
                

              
            }
            // send to caller               
            Clients.Caller.onConnected(id, userName, ConnectedUsers, CurrentMessage);
        }

        public void SendMessageToAll(string Name, string message, string time)
        {
            var user = db.tbl_Users.Where(x => x.UserName == Name).Select(z=> new { id = z.ID,img = z.Photo }).FirstOrDefault();
            Message M = new Message {
                UserID = user.id,
                Message1 = message,
                Time = time,
                UserImage = user.img
        };
            if (M.UserImage == null) {
                M.UserImage = "images/dummy.png";
            }

            db.Messages.Add(M);
            db.SaveChanges();
           string UserImg = GetUserImage(Name);
            // store last 100 messages in cache
            AddMessageinCache(Name, message, time, UserImg);

            // Broad cast message
            Clients.All.messageReceived(Name, message, time, UserImg);

        }

        private void AddMessageinCache(string userName, string message, string time, string UserImg)
        {
            CurrentMessage.Add(new Messages { UserName = userName,Message = message, Time = time, UserImage = UserImg });

            if (CurrentMessage.Count > 100)
                CurrentMessage.RemoveAt(0);

            // Refresh();
        }

        public string GetUserImage(string username)
        {
            string RetimgName = "images/dummy.png";
            try
            {
                string query = "select Photo from tbl_Users where UserName='" + username + "'";
                string ImageName = ConnC.GetColumnVal(query, "Photo");

                if (ImageName != "")
                    RetimgName = ImageName;
            }
            catch (Exception ex)
            { }
            return RetimgName;
        }


        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            var item = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                ConnectedUsers.Remove(item);

                var id = Context.ConnectionId;
                Clients.All.onUserDisconnected(id, item.UserName);

            }
            return base.OnDisconnected(stopCalled);
        }
    }
}