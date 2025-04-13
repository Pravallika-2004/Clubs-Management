using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    public class ClubsController : Controller
    {
        private readonly dummyclubsEntities db = new dummyclubsEntities();
        // GET: Clubs
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ClubsandImages()
        {
            return View();
        }


        public ActionResult Departments()
        {
            // Fetch IFHE university ID
            var ifheUniversity = db.UNIVERSITies.FirstOrDefault(u => u.Abbreviation == "IFHE");

            if (ifheUniversity == null)
            {
                ViewBag.ErrorMessage = "IFHE University not found.";
                return View(new List<DEPARTMENT>()); // Return an empty list
            }

            int ifheUniversityId = ifheUniversity.UniversityID;

            // Fetch departments for IFHE university
            List<DEPARTMENT> departments = db.DEPARTMENTs
                .Where(d => d.Universityid == ifheUniversityId && d.IsActive == true)
                .ToList();

            return View(departments); // Directly return the list
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public JsonResult GetClubLogos(int departmentId)
        {
            var clubs = db.CLUBS
                .Where(c => c.DepartmentID == departmentId)
                .Select(c => new { c.ClubID, c.ClubName, c.LogoImagePath }) // Keep the original path
                .ToList();

            return Json(clubs, JsonRequestBehavior.AllowGet);
        }


        public ActionResult GetClubDetails(int clubId)
        {
            var club = db.CLUBS.Find(clubId);
            return Json(club);
        }

        public ActionResult ClubDetails(int id)
        {
            var club = db.CLUBS.FirstOrDefault(c => c.ClubID == id);
            if (club == null)
            {
                return HttpNotFound();
            }

            // Fetching Department Name based on DepartmentID
            var department = db.DEPARTMENTs.FirstOrDefault(d => d.DepartmentID == club.DepartmentID);

            // Fetching Mentor's Email from Logins table
            var mentorLogin = db.Logins.FirstOrDefault(m => m.LoginID == club.MentorID);

            // Fetching Mentor's Name from Users table using the Email
            var mentorUser = db.USERs.FirstOrDefault(u => u.Email == mentorLogin.Email);

            // Storing values in ViewBag
            ViewBag.DepartmentName = department != null ? department.DepartmentName : "Unknown";
            ViewBag.MentorName = mentorUser != null ? mentorUser.FirstName : "Unknown";
            ViewBag.MentorEmail = mentorLogin != null ? mentorLogin.Email : "Unknown";


            // --- Set University and Departments ---
            // First, fetch the university based on the club's department.
            var university = department != null
                ? db.UNIVERSITies.FirstOrDefault(u => u.UniversityID == department.Universityid)
                : null;

            // Fetch all departments under this university
            var departments = university != null
? db.DEPARTMENTs
.Where(d => d.Universityid == university.UniversityID)
.Select(d => new DepartmentDto { Id = d.DepartmentID, Name = d.DepartmentName })
.ToList()
: new List<DepartmentDto>();



            // Assign values to ViewBag so they are available in the view (and in your modal)
            ViewBag.UniversityName = university?.UniversityNAME ?? "Unknown";
            ViewBag.UniversityId = university?.UniversityID; // Hidden ID for Form Submission
            ViewBag.Departments = departments;
            // --- End of University and Departments assignment ---

            // Optionally, for debugging
            // System.Diagnostics.Debug.WriteLine("University: " + ViewBag.UniversityName);



            return View(club);
        }

        [HttpGet]
        public JsonResult GetUniversityAndDepartments(int clubId)
        {
            var club = db.CLUBS.FirstOrDefault(c => c.ClubID == clubId);
            if (club == null)
            {
                return Json(new { UniversityName = "Unknown", Departments = new List<object>() }, JsonRequestBehavior.AllowGet);
            }

            var department = db.DEPARTMENTs.FirstOrDefault(d => d.DepartmentID == club.DepartmentID);
            var university = db.UNIVERSITies.FirstOrDefault(u => u.UniversityID == department.Universityid);

            var departments = db.DEPARTMENTs
                .Where(d => d.Universityid == university.UniversityID)
                .Select(d => new { Id = d.DepartmentID, Name = d.DepartmentName })
                .ToList();

            return Json(new { UniversityName = university.UniversityNAME, Departments = departments }, JsonRequestBehavior.AllowGet);
        }





        [HttpPost]
        public ActionResult Register(ClubRegistration model, HttpPostedFileBase ProfileImage)
        {

            System.Diagnostics.Debug.WriteLine("This is a trace message");
            System.Diagnostics.Debug.WriteLine("Profile Image: " + (ProfileImage != null ? ProfileImage.FileName : "No file uploaded"));
            System.Diagnostics.Debug.WriteLine("Model Data: " + JsonConvert.SerializeObject(model));
            System.Diagnostics.Debug.WriteLine($"ClubID: {model.ClubID}, DepartmentID: {model.DepartmentID}, UniversityID: {model.UniversityID}");


            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();

                System.Diagnostics.Debug.WriteLine("Validation Errors: " + string.Join(", ", errors));

                return Json(new { success = false, message = "Invalid data!", errors });
            }


            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1️⃣ Check if User Already Exists
                    var existingUser = db.USERs.FirstOrDefault(u => u.Email == model.Email);
                    if (existingUser == null)
                    {
                        existingUser = new USER
                        {
                            FirstName = model.FullName,
                            LastName = "null",
                            Email = model.Email,
                            Password = "hashedPassword",
                            SubscriptionStatus = "normal",
                            RegistrationDate = DateTime.Now,
                            UserType = "campus",
                            Userrole = "student",
                            MobileNumber = model.ContactNumber,
                            WhatsAppNumber = model.ContactNumber,
                            Address = "null",
                            City = "null",
                            State = "null",
                            PinCode = "null",
                            District = "null",
                            IsActive = true,
                            PhotoPath = null,
                            DepartmentID = model.DepartmentID,
                            IsActiveDate = DateTime.Now
                        };

                        db.USERs.Add(existingUser);
                        db.SaveChanges();
                    }

                    // 2️⃣ Fetch the University ID from the Department
                    var department = db.DEPARTMENTs.FirstOrDefault(d => d.DepartmentID == model.DepartmentID);
                    var university = db.UNIVERSITies.FirstOrDefault(u => u.UniversityID == department.Universityid);
                    if (university == null)
                    {
                        return Json(new { success = false, message = "University not found!" });
                    }

                    // 3️⃣ Upload Profile Image
                    if (ProfileImage != null && ProfileImage.ContentLength > 0)
                    {
                        string uploadsFolder = Server.MapPath("~/uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProfileImage.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        ProfileImage.SaveAs(filePath);
                        existingUser.PhotoPath = "/uploads/" + uniqueFileName;
                    }

                    // 4️⃣ Check if User is Already Registered in the Club
                    var existingRegistration = db.ClubRegistrations.FirstOrDefault(cr => cr.UserID == existingUser.UserID && cr.ClubID == model.ClubID);
                    if (existingRegistration != null)
                    {
                        return Json(new { success = false, message = "User already registered for this club!" });
                    }

                    // 5️⃣ Validate Club Seat Availability
                    var club = db.CLUBS.FirstOrDefault(c => c.ClubID == model.ClubID);
                    if (club == null || club.AvailableClubCommitteeSeats <= 0)
                    {
                        return Json(new { success = false, message = "No available seats in this club!" });
                    }

                    // 6️⃣ Save Club Registration
                    var clubRegistration = new ClubRegistration
                    {
                        UserID = existingUser.UserID,
                        ClubID = model.ClubID,
                        FullName = existingUser.FirstName,
                        Email = existingUser.Email,
                        ContactNumber = existingUser.MobileNumber,
                        ProfileImagePath = existingUser.PhotoPath,
                        PreferredRole = model.PreferredRole,
                        RoleJustification = model.RoleJustification,
                        RegisteredAt = DateTime.Now,
                        ApprovalStatusID = 1,
                        DepartmentID = model.DepartmentID,
                        UniversityID = university.UniversityID
                    };

                    db.ClubRegistrations.Add(clubRegistration);

                    // 7️⃣ Reduce Club Seats with **Concurrency Handling**
                    db.Entry(club).Reload(); // Ensure the latest data
                    if (club.AvailableClubCommitteeSeats <= 0)
                    {
                        return Json(new { success = false, message = "Seats are no longer available!" });
                    }
                    club.AvailableClubCommitteeSeats--;

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Registration successful!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                }
            }
        }

        public ActionResult BrowseEvents(int clubId)
        {
            using (var db = new dummyclubsEntities())
            {
                // Fetch Club Name
                var club = db.CLUBS.FirstOrDefault(c => c.ClubID == clubId);
                if (club != null)
                {
                    ViewBag.ClubName = club.ClubName;
                }

                // Get all events that are upcoming
                var events = db.EVENTS
                    .Where(e => e.ClubID == clubId &&
                                (e.EventStatus == "Upcoming not posted" || e.EventStatus == "Upcoming posted"))
                    .ToList();

                // Update those with "Upcoming not posted"
                foreach (var ev in events.Where(e => e.EventStatus == "Upcoming not posted"))
                {
                    ev.IsActive = true;
                    ev.EventStatus = "Upcoming posted";
                }

                db.SaveChanges();

                ViewBag.UpcomingEvents = events;


                // Load comments for all upcoming events of this club
                var eventIds = events.Select(e => e.EventID).ToList();
                ViewBag.Comments = db.Comments.Where(c => eventIds.Contains(c.EventID)).ToList();

                // Set ViewBag.EventID for AJAX (temporary workaround)
                ViewBag.EventID = events.FirstOrDefault()?.EventID ?? 0;
            }

            return View();
        }


        // GET: Events/Details/5
        public ActionResult EventDetails(int id)
        {
            var eventItem = db.EVENTS
    .Include("Comments")
    .Include("Comments.Comments1")
    .FirstOrDefault(e => e.EventID == id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            // Only include top-level comments
            eventItem.Comments = eventItem.Comments
                .Where(c => c.ParentCommentID == null)
                .ToList();

            return View(eventItem);
        }


        [HttpPost]
        public ActionResult AddComment(Comment comment)
        {
            if (ModelState.IsValid)
            {
                comment.PostedDate = DateTime.Now;
                db.Comments.Add(comment);
                db.SaveChanges();
            }

            return RedirectToAction("EventDetails", "Events", new { id = comment.EventID });
        }




    }
}