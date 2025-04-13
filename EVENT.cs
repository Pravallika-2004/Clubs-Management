//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebApplication4.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class EVENT
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EVENT()
        {
            this.Comments = new HashSet<Comment>();
        }
    
        public Nullable<int> ClubID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; }
        public string EventDescription { get; set; }
        public string EventType { get; set; }
        public System.DateTime EventStartDateAndTime { get; set; }
        public System.DateTime EventEndDateAndTime { get; set; }
        public Nullable<System.DateTime> EventCreatedDate { get; set; }
        public string EventPoster { get; set; }
        public string EventBrochure { get; set; }
        public Nullable<int> NoOfLikes { get; set; }
        public Nullable<int> NoOfDislikes { get; set; }
        public string QRContentText { get; set; }
        public string QRImage { get; set; }
        public int EventOrganizerID { get; set; }
        public int ApprovalStatusID { get; set; }
        public string RegistrationURL { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public string EventBannerPath { get; set; }
        public string EventStatus { get; set; }

        [NotMapped]
        public string ClubName;

        [NotMapped]
        public string Department;

        [NotMapped]
        public string University;

        [NotMapped]
        public string OrganizerName;
        public virtual ApprovalStatusTable ApprovalStatusTable { get; set; }
        public virtual CLUB CLUB { get; set; }
        public virtual Login Login { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Comment> Comments { get; set; }
    }
}
