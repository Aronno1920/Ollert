﻿using Ollert.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Ollert.Models;
using System.IO;

namespace Ollert.Controllers
{
    [Authorize]
    public class BoardController : Controller
    {
        //
        // GET: /Board/
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> Salle(int id = -1)
        {
            var currentUserId = this.User.Identity.GetUserId();

            var db = new OllertDbContext();
            var salle = await db.Salles
                .Where(s => s.ParticipantsSalle.Count(p => p.Participant.Id == currentUserId) > 0 || s.Proprietaire.Id == currentUserId)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (salle == null)
                return RedirectToAction("List");

            this.ViewBag.SalleId = salle.Id;
            this.ViewBag.NomSalle = salle.Nom;

            db.Dispose();

            return View();
        }

        #region User Requests
        public async Task<ActionResult> CurrentUser()
        {
            var userManager = new UserManager<OllertUser>(new UserStore<OllertUser>(new OllertDbContext()));
            var currentUser = await userManager.FindByIdAsync(this.User.Identity.GetUserId());

            var result = await Newtonsoft.Json.JsonConvert.SerializeObjectAsync(currentUser);

            return Content(result, "application/json");
        }

        public async Task<ActionResult> AvatarImage(string id)
        {
            var userManager = new UserManager<OllertUser>(new UserStore<OllertUser>(new OllertDbContext()));
            var user = await userManager.FindByIdAsync(id);
            if (user.Avatar == null)
            {
                user.Avatar = System.IO.File.ReadAllBytes(HttpContext.Request.MapPath(HttpContext.Request.ApplicationPath) + "/Content/images/avatar/avatarblue.png");
            }

            return File(user.Avatar, "image/png");
        }

        public async Task<ActionResult> UserLastSeen()
        {
            var db = new OllertDbContext();
            string userId = this.User.Identity.GetUserId();
            var currentUser = await db.Users.FirstAsync(u => u.Id == userId);
            currentUser.LastViewed = DateTime.Now;
            await db.SaveChangesAsync();

            var result = Json(true);
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return result;
        }
        #endregion

        #region File Management
        public async Task<ActionResult> AddFileToCard(HttpPostedFileBase file, int cardId)
        {
            Fichier fichier = null;

            if (file.ContentLength != 0)
            {
                byte[] fileData = null;
                using (var binaryReader = new BinaryReader(file.InputStream))
                {
                    fileData = binaryReader.ReadBytes(file.ContentLength);
                }

                var db = new OllertDbContext();
                var carte = await db.Cartes.FirstAsync(c => c.Id == cardId);
                fichier = new Fichier
                {
                    Data = fileData,
                    Nom = file.FileName,
                    DateEnvoi = DateTime.Now,
                    ContentType = file.ContentType
                };
                carte.Fichiers.Add(fichier);

                await db.SaveChangesAsync();

                // Ajoute une notification
                await Ollert.Services.NotificationService.AddNotification<Fichier>(
                    "Fichier Ajouté", "{0} a ajouté un fichier a la carte : 'Demande {1}'".FormatWith(this.User.Identity.Name, carte.NumeroDemande), 
                    TypeNotification.AjoutFichier,
                    fichier);
            }

            var result = await Newtonsoft.Json.JsonConvert.SerializeObjectAsync(fichier);

            return Content(result, "application/json");
        }

        public async Task<ActionResult> DownloadFile(int id, bool download = false)
        {
            var db = new OllertDbContext();
            var fichier = await db.Fichiers.FirstAsync(f => f.Id == id);

            if (!fichier.ContentType.Contains("image") && !download)
            {
                fichier.Data = System.IO.File.ReadAllBytes(HttpContext.Request.MapPath(HttpContext.Request.ApplicationPath) + "/Content/images/text_enriched.png");
            }

            return File(fichier.Data, fichier.ContentType);
        }
        #endregion
	}
}