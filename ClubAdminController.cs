
using System;
using System.Collections.Generic;
using System.EnterpriseServices.CompensatingResourceManager;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WebApplication4.Models;


namespace WebApplication1.Controllers
{
    public class ClubAdminController : Controller
    {
        private readonly dummyclubsEntities _db = new dummyclubsEntities(); // Database context // Database context

        // GET: ClubAdmin Dashboard (Index)
        public ActionResult Index()
        {
            // Check if the user is logged in
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Admin"); // Redirect to login if not authenticated
            }

            // Get logged-in user's email
            string userEmail = Session["UserEmail"].ToString();

            // Fetch Club Admin details
            var clubAdmin = _db.ClubRegistrations.FirstOrDefault(c => c.Email == userEmail);

            if (clubAdmin == null)
            {
                return HttpNotFound("Club Admin not found");
            }

            // Fetch Club Name from the CLUB table using ClubID
            var club = _db.CLUBS.FirstOrDefault(cl => cl.ClubID == clubAdmin.ClubID);

            int loginId = Convert.ToInt32(Session["UserID"]);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] clubadmin LoginID: {loginId}");


            // Fetch notifications
            var notifications = _db.Notifications
                                   .Where(n => n.LoginID == loginId && n.IsRead == false && n.EndDate > DateTime.Now)
                                   .ToList();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Total Unread Notifications: {notifications.Count}");

            ViewBag.Notifications = notifications;

            // Pass club admin details to the view
            ViewBag.ClubAdminName = clubAdmin.FullName;
            ViewBag.ClubName = club?.ClubName ?? "Not Assigned"; // Show club name or "Not Assigned" if null
            ViewBag.Department = _db.DEPARTMENTs.FirstOrDefault(d => d.DepartmentID == clubAdmin.DepartmentID)?.DepartmentName;
            ViewBag.University = _db.UNIVERSITies.FirstOrDefault(u => u.UniversityID == clubAdmin.UniversityID)?.UniversityNAME;
            ViewBag.ClubAdminPhoto = clubAdmin.ProfileImagePath; // Assuming the photo is stored as a file path or URL


            return View();
        }

        // GET: Request Event Form
        public ActionResult RequestEvent()
        {
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            string userEmail = Session["UserEmail"].ToString();
            var clubAdmin = _db.ClubRegistrations.FirstOrDefault(c => c.Email == userEmail);

            if (clubAdmin == null)
            {
                return HttpNotFound("Club Admin not found");
            }

            var club = _db.CLUBS.FirstOrDefault(c => c.ClubID == clubAdmin.ClubID);
            var department = club != null ? _db.DEPARTMENTs.FirstOrDefault(d => d.DepartmentID == club.DepartmentID) : null;

            var model = new EVENT
            {
                ClubID = clubAdmin.ClubID, // Store ClubID for event creation
                ClubName = club?.ClubName, // Retrieve ClubName safely
                Department = department?.DepartmentName, // Safe null check
                University = _db.UNIVERSITies.FirstOrDefault(u => u.UniversityID == clubAdmin.UniversityID)?.UniversityNAME // Show in view
            };

            ViewBag.OrganizerName = clubAdmin.FullName;

            return View(model);
        }

        // POST: Request Event Submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestEvent(EVENT model, HttpPostedFileBase EventPoster)
        {
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string userEmail = Session["UserEmail"].ToString();
            var clubAdmin = _db.ClubRegistrations.FirstOrDefault(c => c.Email == userEmail);

            if (clubAdmin == null)
            {
                return HttpNotFound("Club Admin not found");
            }

            // Handle Event Poster Upload
            string filePath = null;
            if (EventPoster != null && EventPoster.ContentLength > 0)
            {
                string uploadsFolder = Server.MapPath("~/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(EventPoster.FileName);
                string savePath = Path.Combine(uploadsFolder, uniqueFileName);
                EventPoster.SaveAs(savePath);
                filePath = "/uploads/" + uniqueFileName;

                // ✅ Debugging File Path
                Console.WriteLine("File uploaded successfully: " + filePath);
            }

            // Check if filePath is null before saving
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = "DefaultPath"; // Set a default path if needed
            }

            // Check if the club admin's RegistrationID exists in the Logins table
            var loginRecord = _db.Logins.FirstOrDefault(l => l.Email == userEmail);
            if (loginRecord == null)
            {
                TempData["ErrorMessage"] = "Error: No associated LoginID found for this club admin.";
                return View(model);
            }

            Console.WriteLine("File uploaded successfully: " + filePath);

            var newEvent = new EVENT
            {
                EventName = model.EventName,
                EventDescription = model.EventDescription,
                ClubID = clubAdmin.ClubID,
                EventOrganizerID = loginRecord.LoginID,
                EventType = "Campus",
                ApprovalStatusID = 1,
                EventCreatedDate = DateTime.Now,
                EventStartDateAndTime = model.EventStartDateAndTime,
                EventEndDateAndTime = model.EventEndDateAndTime,
                EventPoster = filePath, // ✅ Ensure this is assigned correctly
                IsActive = false
            };

            // Save event and check if EventPoster is stored
            try
            {
                _db.EVENTS.Add(newEvent);
                int changes = _db.SaveChanges();

                if (changes > 0)
                {
                    Console.WriteLine("Event saved successfully with EventPoster: " + filePath);
                    TempData["SuccessMessage"] = "Event request submitted successfully!";
                    return RedirectToAction("ViewEventStatus");
                }
                else
                {
                    TempData["ErrorMessage"] = "No changes were made.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return View(model);
            }
        }


        public ActionResult MarkNotificationAsRead(int notificationId)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] MarkNotificationAsRead called with ID: {notificationId}");

            var notification = _db.Notifications.FirstOrDefault(n => n.NotificationID == notificationId);

            if (notification != null)
            {
                notification.IsRead = true;  // ✅ Mark as read
                _db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Notification {notificationId} marked as read.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Notification {notificationId} NOT FOUND!");
            }

            return RedirectToAction("Index"); // Refresh dashboard
        }

        public ActionResult UpcomingEvents()
        {
            using (var db = new dummyclubsEntities())
            {
                // Approved but not yet posted to website
                var approvedNotPosted = db.EVENTS
                    .Where(e => e.ApprovalStatusID == 2 && e.IsActive == false)
                    .ToList();

                // Approved and already posted upcoming events (not yet concluded)
                var postedUpcoming = db.EVENTS
                    .Where(e => e.ApprovalStatusID == 2 && e.IsActive == true && e.EventStatus == "Upcoming posted")
                    .ToList();

                var model = new UpcomingEventsViewModel
                {
                    ApprovedNotPostedEvents = approvedNotPosted,
                    PostedUpcomingEvents = postedUpcoming
                };

                return View(model);
            }
        }


        public ActionResult PostEvent(int eventId)
        {
            var ev = _db.EVENTS.Find(eventId);

            if (ev == null)
            {
                return HttpNotFound("Event not found.");
            }

            var model = new PostEventViewModel
            {
                EventID = ev.EventID,
                EventName = ev.EventName,
                EventDescription = ev.EventDescription,
                EventStartDateAndTime = ev.EventStartDateAndTime,
                EventEndDateAndTime = ev.EventEndDateAndTime,
                RegistrationURL = ev.RegistrationURL,
                QRContentText = ev.QRContentText,
                OrganizerName = ev.OrganizerName,
                ClubName = ev.ClubName,
                EventBanner = ev.EventBannerPath
            };

            return View(model);
        }

      
        [HttpPost]
        public ActionResult PostEvent(PostEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure the UploadedPosters directory exists
                    var posterDirectory = Server.MapPath("~/UploadedPosters");
                    if (!Directory.Exists(posterDirectory))
                    {
                        Directory.CreateDirectory(posterDirectory);
                    }
              

                    // Save uploaded poster
                    if (model.EventPosterFile != null && model.EventPosterFile.ContentLength > 0)
                    {
                        var posterFileName = Path.GetFileName(model.EventPosterFile.FileName);
                        var posterPath = Path.Combine(posterDirectory, posterFileName);
                        model.EventPosterFile.SaveAs(posterPath);
                        model.EventPoster = "/UploadedPosters/" + posterFileName;
                    }

                    // Ensure the UploadedBanners directory exists
                    var bannerDirectory = Server.MapPath("~/UploadedBanners");
                    if (!Directory.Exists(bannerDirectory))
                    {
                        Directory.CreateDirectory(bannerDirectory);
                    }

                    // Save uploaded banner
                    if (model.EventBannerFile != null && model.EventBannerFile.ContentLength > 0)
                    {
                        var bannerFileName = Path.GetFileName(model.EventBannerFile.FileName);
                        var bannerPath = Path.Combine(bannerDirectory, bannerFileName);
                        model.EventBannerFile.SaveAs(bannerPath);
                        model.EventBanner = "/UploadedBanners/" + bannerFileName;
                    }

                    // Update event in DB
                    var ev = _db.EVENTS.Find(model.EventID);
                    if (ev != null)
                    {
                        ev.EventDescription = model.EventDescription;
                        ev.EventStartDateAndTime = model.EventStartDateAndTime;
                        ev.EventEndDateAndTime = model.EventEndDateAndTime;
                        ev.EventPoster = model.EventPoster;
                        ev.EventBannerPath = model.EventBanner;
                        ev.EventStatus = "Upcoming not posted";

                        // Debug output
                        System.Diagnostics.Debug.WriteLine("===== Event Details Before Save =====");
                        System.Diagnostics.Debug.WriteLine("EventID: " + ev.EventID);
                        System.Diagnostics.Debug.WriteLine("EventDescription: " + ev.EventDescription);
                        System.Diagnostics.Debug.WriteLine("EventStartDateAndTime: " + ev.EventStartDateAndTime);
                        System.Diagnostics.Debug.WriteLine("EventEndDateAndTime: " + ev.EventEndDateAndTime);
                        System.Diagnostics.Debug.WriteLine("EventPoster: " + ev.EventPoster);
                        System.Diagnostics.Debug.WriteLine("EventBannerPath: " + ev.EventBannerPath);
                        System.Diagnostics.Debug.WriteLine("EventStatus: " + ev.EventStatus);
                        System.Diagnostics.Debug.WriteLine("=====================================");

                        _db.SaveChanges();
                    }


                    ViewBag.Message = "Event posted successfully!";
                    return View("PostEventSuccess", model);
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var entityValidationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in entityValidationErrors.ValidationErrors)
                        {
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                            // Optional: Log to console or file for debugging
                            System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                        }
                    }
                }

                catch (Exception ex)
                {
                    // Log the exception details
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }

            // If we reach here, something went wrong; redisplay the form
            return View(model);
        }


        private string DetermineEventStatus(DateTime start, DateTime end)
        {
            if (DateTime.Now < start)
                return "Upcoming";
            else if (DateTime.Now >= start && DateTime.Now <= end)
                return "Ongoing";
            else
                return "Completed";
        }

        public ActionResult EditEvent(int eventId)
        {
            var ev = _db.EVENTS.Find(eventId);

            if (ev == null)
            {
                return HttpNotFound("Event not found.");
            }

            // Step 1: Get ClubName from ClubID
            string clubName = string.Empty;
            if (ev.ClubID != null)
            {
                var club = _db.CLUBS.FirstOrDefault(c => c.ClubID == ev.ClubID);
                if (club != null)
                {
                    clubName = club.ClubName;
                }
            }

            // Step 2: Get Organizer Email from LOGIN table using OrganizerID
            string organizerName = string.Empty;
            var login = _db.Logins.FirstOrDefault(l => l.LoginID == ev.EventOrganizerID);
            if (login != null)
            {
                // Step 3: Get Organizer Name from CLUBREGISTRATIONS using Email
                var clubReg = _db.ClubRegistrations.FirstOrDefault(cr => cr.Email == login.Email);
                if (clubReg != null)
                {
                    organizerName = clubReg.FullName;
                }
            }

            var model = new PostEventViewModel
            {
                EventID = ev.EventID,
                EventName = ev.EventName,
                EventDescription = ev.EventDescription,
                EventStartDateAndTime = ev.EventStartDateAndTime,
                EventEndDateAndTime = ev.EventEndDateAndTime,
                RegistrationURL = ev.RegistrationURL,
                QRContentText = ev.QRContentText,
                OrganizerName = organizerName,
                ClubName = clubName,
                EventPoster=ev.EventPoster,
                EventBanner=ev.EventBannerPath,
                Venue="ICFAI Foundation,Hydreabad",
               // EventBanner = ev.EventBannerPath
            };

            return View(model);
        }



        [HttpPost]
        public ActionResult EditEvent(PostEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure the UploadedPosters directory exists
                    var posterDirectory = Server.MapPath("~/UploadedPosters");
                    if (!Directory.Exists(posterDirectory))
                    {
                        Directory.CreateDirectory(posterDirectory);
                    }


                    // Save uploaded poster
                    if (model.EventPosterFile != null && model.EventPosterFile.ContentLength > 0)
                    {
                        var posterFileName = Path.GetFileName(model.EventPosterFile.FileName);
                        var posterPath = Path.Combine(posterDirectory, posterFileName);
                        model.EventPosterFile.SaveAs(posterPath);
                        model.EventPoster = "/UploadedPosters/" + posterFileName;
                    }

                    // Ensure the UploadedBanners directory exists
                    var bannerDirectory = Server.MapPath("~/UploadedBanners");
                    if (!Directory.Exists(bannerDirectory))
                    {
                        Directory.CreateDirectory(bannerDirectory);
                    }

                    // Save uploaded banner
                    if (model.EventBannerFile != null && model.EventBannerFile.ContentLength > 0)
                    {
                        var bannerFileName = Path.GetFileName(model.EventBannerFile.FileName);
                        var bannerPath = Path.Combine(bannerDirectory, bannerFileName);
                        model.EventBannerFile.SaveAs(bannerPath);
                        model.EventBanner = "/UploadedBanners/" + bannerFileName;
                    }

                    // Update event in DB
                    var ev = _db.EVENTS.Find(model.EventID);
                    if (ev != null)
                    {
                        ev.EventDescription = model.EventDescription;
                        ev.EventStartDateAndTime = model.EventStartDateAndTime;
                        ev.EventEndDateAndTime = model.EventEndDateAndTime;
                        ev.EventPoster = model.EventPoster;
                        ev.EventBannerPath = model.EventBanner;
                       // ev.EventStatus = "Upcoming not posted";

                        // Debug output
                        System.Diagnostics.Debug.WriteLine("===== Event Details Before Save =====");
                        System.Diagnostics.Debug.WriteLine("EventID: " + ev.EventID);
                        System.Diagnostics.Debug.WriteLine("EventDescription: " + ev.EventDescription);
                        System.Diagnostics.Debug.WriteLine("EventStartDateAndTime: " + ev.EventStartDateAndTime);
                        System.Diagnostics.Debug.WriteLine("EventEndDateAndTime: " + ev.EventEndDateAndTime);
                        System.Diagnostics.Debug.WriteLine("EventPoster: " + ev.EventPoster);
                        System.Diagnostics.Debug.WriteLine("EventBannerPath: " + ev.EventBannerPath);
                        System.Diagnostics.Debug.WriteLine("EventStatus: " + ev.EventStatus);
                        System.Diagnostics.Debug.WriteLine("=====================================");

                        _db.SaveChanges();
                    }


                    ViewBag.Message = "Event posted successfully!";
                    return View("PostEventSuccess", model);
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var entityValidationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in entityValidationErrors.ValidationErrors)
                        {
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                            // Optional: Log to console or file for debugging
                            System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                        }
                    }
                }

                catch (Exception ex)
                {
                    // Log the exception details
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }

            // If we reach here, something went wrong; redisplay the form
            return View(model);
        }


    }
}
















































































































































