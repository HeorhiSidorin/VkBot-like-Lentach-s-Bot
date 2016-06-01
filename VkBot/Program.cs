using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model.RequestParams;

namespace VkBot
{
    class Program
    {

        private static void CommentsControl(dynamic state)
        {

            dynamic[] array = state as dynamic[];

            WallGetCommentsParams commentsparams = new WallGetCommentsParams();
            commentsparams.NeedLikes = true;
            commentsparams.OwnerId = array[3];
            commentsparams.PostId = array[1].Id;
            commentsparams.Sort = SortOrderBy.Asc;

            int totalCount;

            try
            {
                var comments = array[0].Wall.GetComments(out totalCount, commentsparams);
                if(comments.Count != 0)
                {
                    Thread.Sleep(501);
                    commentsparams.Count = totalCount;
                    comments = array[0].Wall.GetComments(out totalCount, commentsparams);
                    Thread.Sleep(501);

                    for (int i = 0; i < comments.Count; i++)
                    {
                        DateTime now = DateTime.Now;
                        long minutes = (now - comments[i].Date).Minutes;
                        if (/*minutes > 2 &&*/ comments[i].Likes.Count < 1)
                        {
                            Console.WriteLine("Deleted: " + comments[i].Text + " from: " + array[1].Text + " " + array[1].Id);
                            try
                            {
                                array[0].Wall.DeleteComment(array[3], comments[i].Id);
                            }
                            catch (VkApiException ex)
                            {
                                Console.WriteLine("Oops... " + ex.Message);
                            }
                        }
                        Thread.Sleep(501);
                    }
                }         
            }
            catch(VkApiException ex)
            {
                Console.WriteLine("Oops... " + ex.Message);
            }     
            array[2].Set();
        }

        static void Main(string[] args)
        {
            VkApi vk = new VkApi();
            for (;;)
            {
                var authparams = new ApiAuthParams();
                authparams.ApplicationId = 5362445;
                Console.WriteLine("Your login");
                authparams.Login = Console.ReadLine();        
                Console.WriteLine("Your password");
                authparams.Password = "";
                
                while (true)
                {
                    var cki = Console.ReadKey(true);
                    if (cki.Key == ConsoleKey.Enter)
                        break;
                    else if(cki.Key == ConsoleKey.Backspace && authparams.Password.Length > 0)
                    {
                        authparams.Password.Substring(0, (authparams.Password.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if(cki.Key != ConsoleKey.Backspace)
                    {
                        Console.Write('*');
                        authparams.Password += cki.KeyChar;
                    }
                    
                };

                Console.WriteLine();

                authparams.Settings = Settings.All;

                try
                {
                    vk.Authorize(authparams);
                    break;
                }
                catch (VkNet.Exception.CaptchaNeededException ex)
                {
                    authparams.CaptchaSid = ex.Sid;
                    Process.Start("" + ex.Img);
                    Console.WriteLine("What do you see?");
                    authparams.CaptchaKey = Console.ReadLine();
                }
                catch(VkApiAuthorizationException ex)
                {
                    Console.WriteLine(ex.Message + "! Try again, please.");
                }
            }     
            
            Console.WriteLine("Cool. Don't worry about my work.");
            long? destinationId = 0;

            for (;;)
            {
                Console.WriteLine("Do i guard your wall(PRINT '1') or your public page's wall(PRINT '2') ?");
                char choose = Console.ReadKey(true).KeyChar;


                if (choose == '2')
                {
                    Console.WriteLine("Type short name of your public page:");
                    var group = new string[] { (string)Console.ReadLine() };
                    try
                    {
                        destinationId = -vk.Groups.GetById(group, group[0], GroupsFields.All)[0].Id;
                        break;
                    }
                    catch(VkApiException ex)
                    {
                        Console.WriteLine("Oops... " + ex.Message);
                    }
                }
                else if (choose == '1')
                {
                    destinationId = vk.UserId;
                    break;
                }
                else
                {
                    Console.WriteLine("Are you joking with me? May be only 2(two) answers: 1 or 2. Understand? Ok. God's love be with you!");
                }
            }
            Console.WriteLine("I'm working. If you want to stop me, just close program.");
            for (;;)
            {
                try
                {
                    WallGetParams wallparams = new WallGetParams();
                    
                    wallparams.OwnerId = destinationId;
                    wallparams.Filter = WallFilter.All;

                    var posts = vk.Wall.Get(wallparams);
                    Thread.Sleep(501);
                    int count = 0;
                    wallparams.Count = posts.TotalCount;
                    posts = vk.Wall.Get(wallparams);
                    Thread.Sleep(501);

                    while (count < (int)posts.WallPosts.Count)
                    {
                        var events = new List<ManualResetEvent>();
                        var resetEvent1 = new ManualResetEvent(false);
                        ThreadPool.QueueUserWorkItem(CommentsControl, new object[] { vk, posts.WallPosts[count++], resetEvent1, destinationId });
                        events.Add(resetEvent1);

                        var resetEvent2 = new ManualResetEvent(false);
                        if (count == (int)posts.WallPosts.Count)
                        {
                            WaitHandle.WaitAll(events.ToArray());
                            break;
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem(CommentsControl, new object[] { vk, posts.WallPosts[count++], resetEvent2, destinationId });
                            events.Add(resetEvent2);
                        }

                        WaitHandle.WaitAll(events.ToArray());
                        Thread.Sleep(501);
                    }
                }
                catch(VkApiException ex)
                {
                    Console.WriteLine("Oops... " + ex.Message);
                }
            }         
        }
    }
}
