﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Microsoft.Win32;
using WebApplication4.Models;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApplication4.Controllers
{
    public class MentorController : Controller
    {
        private readonly dummyclubsEntities _db = new dummyclubsEntities();
        private readonly EmailService _emailService = new EmailService();  // Injecting EmailService

        // ✅ Mentor Dashboard
        public ActionResult Index()
        {
            if (!IsMentorLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            int mentorID = GetMentorID();  // ✅ Fetch Mentor ID

            // ✅ Debug: Check if Mentor ID is retrieved correctly
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Mentor LoginID: {mentorID}");

            // Fetch mentor details
            var mentor = _db.Logins.FirstOrDefault(m => m.LoginID == mentorID && m.Role == "Mentor");
            if (mentor == null)
            {
                TempData["ErrorMessage"] = "Mentor not found!";
                return RedirectToAction("Login", "Admin");
            }

            // Fetch university details
            var university = _db.UNIVERSITies.FirstOrDefault(u => u.UniversityID == mentor.UniversityID);
            if (university == null)
            {
                TempData["ErrorMessage"] = "University not assigned!";
                return RedirectToAction("Login", "Admin");
            }

            // ✅ Debug: Check if university data is retrieved
            System.Diagnostics.Debug.WriteLine($"[DEBUG] University ID: {university.UniversityID}, Name: {university.UniversityNAME}");

            // Fetch notifications
            var notifications = _db.Notifications
                                   .Where(n => n.LoginID == mentorID && n.IsRead == false && n.EndDate > DateTime.Now)
                                   .ToList();

            // ✅ Debug: Check if notifications are retrieved
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Total Unread Notifications: {notifications.Count}");

            // Pass data to the view
            ViewBag.Mentor = mentor;
            ViewBag.University = university;
            ViewBag.Notifications = notifications;

            return View();
        }


        // ✅ Club Registration (Mentors can register clubs under their university)
        //public ActionResult RegisterClub()
        //{
        //    if (!IsMentorLoggedIn())
        //    {
        //        return RedirectToAction("Login", "Admin");
        //    }

        //    return View(new CLUB()); // Create new club model
        //}

        //[HttpPost]
        //public ActionResult RegisterClub(CLUB club)
        //{
        //    if (!IsMentorLoggedIn())
        //    {
        //        return RedirectToAction("Login", "Admin");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.ErrorMessage = "Invalid input. Please fill all required fields.";
        //        return View(club);
        //    }

        //    try
        //    {
        //        int universityID = GetUniversityID();
        //        int mentorID = GetMentorID();

        //        club.UniversityID = universityID;
        //        club.MentorID = mentorID;
        //        club.CreatedDate = DateTime.Now;
        //        club.IsActive = true;

        //        _db.CLUBs.Add(club);
        //        _db.SaveChanges();

        //        TempData["SuccessMessage"] = "Club registered successfully!";
        //        return RedirectToAction("ManageClubs");
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Error in RegisterClub: {ex.Message}");
        //        ViewBag.ErrorMessage = "An error occurred while registering the club.";
        //        return View(club);
        //    }
        //}

        //// ✅ Manage Clubs (Mentors can view clubs they registered)
        //public ActionResult ManageClubs()
        //{
        //    if (!IsMentorLoggedIn())
        //    {
        //        return RedirectToAction("Login", "Admin");
        //    }

        //    int mentorID = GetMentorID();
        //    var clubs = _db.CLUBs.Where(c => c.MentorID == mentorID).ToList();

        //    return View(clubs);
        //}

        // ✅ Utility: Check if Mentor is Logged In
        private bool IsMentorLoggedIn()
        {
            return Session["UserRole"] != null &&
                   (string)Session["UserRole"] == "Mentor" &&
                   Session["UserID"] != null;
        }

        // ✅ Utility: Get Mentor ID from Session
        private int GetMentorID()
        {
            return Convert.ToInt32(Session["UserID"]);
        }

        // ✅ Utility: Get University ID from Session
        private int GetUniversityID()
        {
            return Convert.ToInt32(Session["UniversityID"]);
        }

        public ActionResult RegisterClub()
        {
            if (!IsMentorLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            int mentorID = GetMentorID(); // Get Mentor ID

            // Fetch mentor's department
            var mentor = _db.Logins.FirstOrDefault(m => m.LoginID == mentorID && m.Role == "Mentor");

            if (mentor == null)
            {
                TempData["ErrorMessage"] = "Mentor not found!";
                return RedirectToAction("Login", "Admin");
            }

            // Fetch department details
            var department = _db.DEPARTMENTs.FirstOrDefault(d => d.DepartmentID == mentor.DepartmentID);

            if (department == null)
            {
                TempData["ErrorMessage"] = "Department not assigned!";
                return RedirectToAction("Login", "Admin");
            }

            // Pass department details to the view
            ViewBag.DepartmentName = department.DepartmentName;
            ViewBag.DepartmentID = department.DepartmentID;

            return View(new CLUB() { DepartmentID = department.DepartmentID }); // Pre-select the department
        }

        [HttpPost]
        public ActionResult RegisterClub(CLUB club)
        {
            if (!IsMentorLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            int mentorID = GetMentorID(); // Get Mentor ID

            // Fetch mentor's department
            var mentor = _db.Logins.FirstOrDefault(m => m.LoginID == mentorID && m.Role == "Mentor");
            var department = _db.DEPARTMENTs.FirstOrDefault(d => d.DepartmentID == mentor.DepartmentID);

            if (!ModelState.IsValid)
            {
                // Re-fetch departments and return the view with an error message
                ViewBag.ErrorMessage = "Invalid input. Please fill all required fields.";
                ViewBag.DepartmentName = department.DepartmentName;
                ViewBag.DepartmentID = department.DepartmentID;

                return View(club);
            }

            try
            {
                // Set club properties
                club.MentorID = mentorID;
                club.CreatedDate = DateTime.Now;
                club.IsActive = false; // Initially inactive until admin approval

                // Handle logo file upload
                if (Request.Files["LogoImage"] != null && Request.Files["LogoImage"].ContentLength > 0)
                {
                    var file = Request.Files["LogoImage"];
                    var fileName = Path.GetFileName(file.FileName);

                    // Define the folder path where the file will be stored
                    string uploadFolder = Server.MapPath("~/Uploads");

                    // Create the directory if it does not exist
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    // Define the file path inside the Uploads folder
                    var filePath = Path.Combine(uploadFolder, fileName);
                    file.SaveAs(filePath);

                    // Store relative file path in DB
                    club.LogoImagePath = "/Uploads/" + fileName;
                }

                // Set ApprovalStatusID to 'PENDING'
                club.ApprovalStatusID = _db.ApprovalStatusTables
                    .FirstOrDefault(a => a.Status == "PENDING")?.ApprovalStatusID ?? 1;

                // Add to the database
                _db.CLUBS.Add(club);
                _db.SaveChanges();

                // Clear ModelState to reset form fields
                ModelState.Clear();

                // Provide success message in the same view
                ViewBag.SuccessMessage = "Club registration request sent to admin!";
                ViewBag.DepartmentName = department.DepartmentName;
                ViewBag.DepartmentID = department.DepartmentID;
                return View(new CLUB() { DepartmentID = department.DepartmentID });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RegisterClub: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while registering the club.";
                ViewBag.DepartmentName = department.DepartmentName;
                ViewBag.DepartmentID = department.DepartmentID;
                return View(club);
            }
        }



        private int GetLoginID()
        {
            if (Session["UserID"] != null)
            {
                return Convert.ToInt32(Session["UserID"]); // Return the stored LoginID
            }
            return 0; // Return 0 if no user is logged in
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


        public ActionResult ViewClubStatus()
        {
            int loggedInMentorID = (int)Session["UserID"]; // Ensure mentor ID is stored in session
            var clubs = _db.CLUBS
                           .Include(c => c.DEPARTMENT.UNIVERSITY)
                           .Where(c => c.MentorID == loggedInMentorID)
                           .ToList();

            var notifications = _db.Notifications
                                   .Where(n => n.LoginID == loggedInMentorID)
                                   .ToList();

            ViewBag.Notifications = notifications;
            return View(clubs);
        }


           
        //[Authorize] // Ensures only logged-in users can access
        public ActionResult ViewClubRegistrations()
        {
            // Get the logged-in Mentor's UserID from session
            if (Session["UserID"] == null)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            int loggedInMentorID = (int)Session["UserID"]; // Corrected variable name

            // Get clubs where the logged-in mentor is assigned
            var clubs = _db.CLUBS.Where(c => c.MentorID == loggedInMentorID)
                                 .Select(c => new { c.ClubID, c.ClubName })
                                 .ToList();

            ViewBag.Clubs = new SelectList(clubs, "ClubID", "ClubName");

            return View();
        }

        // Fetch registrations dynamically based on the selected club
        public ActionResult GetClubRegistrations(int clubId)
        {
            var registrations = _db.ClubRegistrations
                       .Where(r => r.ClubID == clubId)
                       .ToList() // Materialize the query before using .ToString()
                       .Select(r => new
                       {
                           r.RegistrationID,
                           r.FullName,
                           r.Email,
                           r.ContactNumber,
                           r.PreferredRole,
                           r.RoleJustification,
                           r.ProfileImagePath,
                           r.AssignedRole,
                           RegisteredAt = r.RegisteredAt.HasValue
                               ? r.RegisteredAt.Value.ToString("yyyy-MM-dd HH:mm")
                               : null
                       })
                       .ToList(); // Convert to list after transformation

            return Json(registrations, JsonRequestBehavior.AllowGet);
        }



      [HttpPost]


        public async Task<ActionResult> AssignRole(int registrationId, string role)
        {
            var registration = await _db.ClubRegistrations.FindAsync(registrationId);
            if (registration == null)
            {
                return View("Error");
            }

            registration.AssignedRole = role;
            registration.ApprovalStatusID = 2;
            await _db.SaveChangesAsync();

            // If the role is "Club Admin", store credentials and send an email
            if (role == "Club Admin")
            {
                var existingUser = await _db.Logins.FirstOrDefaultAsync(l => l.Email == registration.Email);
                if (existingUser == null)
                {
                    // New login entry
                    var newUser = new Login
                    {
                        Email = registration.Email,
                        PasswordHash = "clubadmin@123", // Default password
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                        UniversityID = registration.UniversityID,  // Ensure these values are correct
                        DepartmentID = registration.DepartmentID,
                        Role = "Club Admin"
                    };

                    _db.Logins.Add(newUser);
                    await _db.SaveChangesAsync();

                    // ✅ Send welcome email
                    string subject = "Club Admin Role Assigned";
                    string body = $"Hello {registration.FullName},<br/><br/>" +
                                  $"You have been assigned as a Club Admin. Here are your login details:<br/>" +
                                  $"<strong>Username:</strong> {registration.Email}<br/>" +
                                  $"<strong>Password:</strong> clubadmin@123 (Please change your password upon login).<br/><br/>" +
                                  "Please log in and update your profile.";

                    await _emailService.SendEmailAsync(registration.Email, subject, body);
                }
            }

            return Json(new { success = true });
        }



        [HttpPost]
        public ActionResult LeaveClub(int registrationId)
        {
            var registration = _db.ClubRegistrations.Find(registrationId);
            if (registration != null)
            {
                registration.AssignedRole = "Club Member"; // Set role to "Club Member"
                registration.ApprovalStatusID = 2;
                _db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        



        // GET: View Event Requests
        public ActionResult ViewEventRequests()
        {
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            string userEmail = Session["UserEmail"].ToString();           
            var mentorLogin = _db.Logins.FirstOrDefault(l => l.Email == userEmail);

            if (mentorLogin == null)
            {
                return HttpNotFound("Mentor login not found");
            }

            ViewBag.Clubs = _db.CLUBS
                .Where(c => c.MentorID == mentorLogin.LoginID)
                .ToList();

            return View(); // No model is passed initially
        }


        // POST: View Event Requests (Filtered)
        [HttpPost]
        public ActionResult ViewEventRequests(int selectedClubId)
        {
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            string userEmail = Session["UserEmail"].ToString();
            var mentorLogin = _db.Logins.FirstOrDefault(l => l.Email == userEmail);

            if (mentorLogin == null)
            {
                return HttpNotFound("Mentor login not found");
            }

            ViewBag.Clubs = _db.CLUBS
                .Where(c => c.MentorID == mentorLogin.LoginID)
                .ToList();

            var events = _db.EVENTS
                .Where(e => e.ClubID == selectedClubId && e.ApprovalStatusID == 1)
                .ToList();

            // Fetch Organizer Names and Club Names separately and store them in ViewBag
            var loginIds = events.Select(e => e.EventOrganizerID).Distinct().ToList();

            var loginIdToEmail = _db.Logins
                .Where(l => loginIds.Contains(l.LoginID))
                .ToDictionary(l => l.LoginID, l => l.Email);

            var emailToName = _db.ClubRegistrations
                .Where(cr => loginIdToEmail.Values.Contains(cr.Email))
                .ToDictionary(cr => cr.Email, cr => cr.FullName);

            // Final dictionary: LoginID => FullName
            ViewBag.OrganizerNames = loginIdToEmail
                .Where(le => emailToName.ContainsKey(le.Value))
                .ToDictionary(le => le.Key.ToString(), le => emailToName[le.Value]);


            ViewBag.ClubNames = _db.CLUBS
                .ToDictionary(c => c.ClubID, c => c.ClubName);

            ViewBag.UniversityName = ViewBag.University; // Use ViewBag for university name

            ViewBag.Universities = _db.UNIVERSITies.ToList(); // Setting universities list to ViewBag

            return View(events); // Pass events list as the model
        }


        // POST: Approve Event Request
        [HttpPost]
        public ActionResult ApproveEventRequest(int eventId)
        {
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var eventToApprove = _db.EVENTS.Find(eventId);
            if (eventToApprove == null)
            {
                return HttpNotFound("Event not found");
            }

            eventToApprove.ApprovalStatusID = 2; // 2 = Approved
            //eventToApprove.IsActive = true;

            try
            {
                _db.SaveChanges();

                // ✅ OrganizerID is the Club Admin's LoginID
                if (eventToApprove.EventOrganizerID == null)
                {
                    TempData["ErrorMessage"] = "Club Admin (Organizer) ID not found. Notification not sent.";
                }
                else
                {
                    var notification = new Notification
                    {
                        LoginID = eventToApprove.EventOrganizerID,
                        Message = $" Your event '{eventToApprove.EventName}' has been approved!",
                        IsRead = false,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddDays(7),
                        CreatedDate = DateTime.Now
                    };

                    _db.Notifications.Add(notification);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Event approved successfully! Notification sent to the club admin.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while approving the event: " + ex.Message;
            }

            return RedirectToAction("ViewEventRequests");
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectEventRequest(int eventId, string rejectionReason)
        {
            if (Session["UserEmail"] == null)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Session expired. Please login again." });

                return RedirectToAction("Login", "Admin");
            }

            var ev = _db.EVENTS.FirstOrDefault(e => e.EventID == eventId);

            if (ev == null)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Event not found!" });

                TempData["ErrorMessage"] = "Event not found!";
                return RedirectToAction("ViewEventRequests");
            }

            ev.ApprovalStatusID = 3; // Rejected
            _db.SaveChanges();

            if (string.IsNullOrWhiteSpace(rejectionReason))
                rejectionReason = "No specific reason provided.";

            if (ev.EventOrganizerID == null)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Club Admin (Organizer) ID not found. Notification not sent." });

                TempData["ErrorMessage"] = "Club Admin (Organizer) ID not found. Notification not sent.";
                return RedirectToAction("ViewEventRequests");
            }

            var notification = new Notification
            {
                LoginID = ev.EventOrganizerID,
                Message = $"❌ Your event '{ev.EventName}' was rejected.\nReason: {rejectionReason}",
                IsRead = false,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7),
                CreatedDate = DateTime.Now
            };

            _db.Notifications.Add(notification);
            _db.SaveChanges();

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true });
            }

            TempData["SuccessMessage"] = $"Event '{ev.EventName}' rejected successfully! Notification sent to club admin.";
            return RedirectToAction("ViewEventRequests");
        }

    }
}
