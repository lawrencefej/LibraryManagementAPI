﻿using System;
using System.ComponentModel.DataAnnotations;

namespace LMSEntities.DataTransferObjects
{
    public class AddAdminDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        [Required]
        public string Role { get; set; }

        public DateTime Created { get; set; }

        public string CallbackUrl { get; set; }

        public AddAdminDto()
        {
            UserName = Email;
            Created = DateTime.Now;
        }
    }
}
