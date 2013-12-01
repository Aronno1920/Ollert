﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Ollert.Models
{
    [DataContract]
    public class Message : Ollert.Models.IEntity
    {
        [Key]
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Texte { get; set; }
        [DataMember]
        public DateTime CreateOn { get; set; }
        [DataMember]
        public virtual OllertUser Utilisateur { get; set; }
        public virtual Carte Carte { get; set; }
    }
}
