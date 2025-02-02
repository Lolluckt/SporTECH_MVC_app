using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainerController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "SalesManagerPolicy")]
        public IActionResult Index(string searchQuery, string sortField = "Name", string sortOrder = "asc")
        {

            var trainers = _context.Trainers
                .Include(t => t.Inventory)
                .AsQueryable();

            trainers = sortField switch
            {
                "Price" => sortOrder == "asc" ? trainers.OrderBy(t => t.Price) : trainers.OrderByDescending(t => t.Price),
                "Quantity" => sortOrder == "asc" ? trainers.OrderBy(t => t.Quantity) : trainers.OrderByDescending(t => t.Quantity),
                "Type" => sortOrder == "asc" ? trainers.OrderBy(t => t.Type) : trainers.OrderByDescending(t => t.Type),
                _ => sortOrder == "asc" ? trainers.OrderBy(t => t.Name) : trainers.OrderByDescending(t => t.Name),
            };

            if (!string.IsNullOrEmpty(searchQuery))
            {
                trainers = trainers.Where(t => t.Name.Contains(searchQuery));
            }

            ViewBag.SearchQuery = searchQuery;

            return View(trainers.ToList());
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var trainer = _context.Trainers.Include(t => t.Inventory).FirstOrDefault(t => t.Id == id);
            if (trainer == null)
            {
                return NotFound();
            }
            return View(trainer);
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Trainer());
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost]
        public IActionResult Create(Trainer model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingTrainer = _context.Trainers.FirstOrDefault(t => t.Name == model.Name);
            if (existingTrainer != null)
            {
                ModelState.AddModelError("Name", "Тренажер с таким названием уже существует.");
                return View(model);
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot/images");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                model.ImagePath = $"/images/{uniqueFileName}";
            }

            _context.Trainers.Add(model);
            _context.SaveChanges();

            var inventory = new Inventory
            {
                TrainerId = model.Id,
                Quantity = model.Quantity
            };
            _context.Inventory.Add(inventory);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var trainer = _context.Trainers.Include(t => t.Inventory).FirstOrDefault(t => t.Id == id);
            if (trainer == null)
            {
                return NotFound();
            }
            return View(trainer);
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost]
        public IActionResult Edit(Trainer model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var trainer = _context.Trainers.Include(t => t.Inventory).FirstOrDefault(t => t.Id == model.Id);
            if (trainer == null)
            {
                return NotFound();
            }

            trainer.Name = model.Name;
            trainer.Description = model.Description;
            trainer.Type = model.Type;
            trainer.ProductionCost = model.ProductionCost;
            trainer.Price = model.Price;
            trainer.Quantity = model.Quantity;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot/images");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(ImageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                if (!string.IsNullOrEmpty(trainer.ImagePath))
                {
                    var oldImagePath = Path.Combine("wwwroot", trainer.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                trainer.ImagePath = $"/images/{uniqueFileName}";
            }

            if (trainer.Inventory != null)
            {
                trainer.Inventory.Quantity = model.Quantity;
            }

            _context.Trainers.Update(trainer);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var trainer = _context.Trainers.Include(t => t.Inventory).FirstOrDefault(t => t.Id == id);
            if (trainer == null)
            {
                return NotFound();
            }

            if (trainer.Inventory != null)
            {
                _context.Inventory.Remove(trainer.Inventory);
            }

            if (!string.IsNullOrEmpty(trainer.ImagePath))
            {
                var oldImagePath = Path.Combine("wwwroot", trainer.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _context.Trainers.Remove(trainer);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
