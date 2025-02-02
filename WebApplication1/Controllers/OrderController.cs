using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Text.RegularExpressions;

namespace WebApplication1.Controllers
{
    [Authorize(Policy = "SalesManagerPolicy")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(string clientName, string trainerName, string phoneNumber, string status)
        {
            var query = _context.Orders
                .Include(o => o.ClientDetails)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Trainer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(clientName))
            {
                var searchName = clientName.Trim().ToLower();
                query = query.Where(o =>
                    (o.ClientDetails.FirstName + " " + o.ClientDetails.LastName).ToLower().Contains(searchName) ||
                    o.ClientDetails.FirstName.ToLower().StartsWith(searchName) ||
                    o.ClientDetails.LastName.ToLower().StartsWith(searchName));
            }

            if (!string.IsNullOrWhiteSpace(trainerName))
            {
                var searchTrainer = trainerName.Trim().ToLower();
                query = query.Where(o => o.OrderDetails.Any(od => od.Trainer.Name.ToLower().StartsWith(searchTrainer)));
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var searchPhone = phoneNumber.Trim();
                query = query.Where(o => o.ClientDetails.PhoneNumber.Contains(searchPhone));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = query.ToList();

            if (!orders.Any())
            {
                ViewBag.SearchMessage = "По запиту нічого не знайдено.";
            }

            return View(orders);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var trainers = _context.Trainers
                .Include(t => t.Inventory)
                .ToList();

            ViewBag.Trainers = trainers;

            return View();
        }

        [HttpPost]
        public IActionResult Create(string firstName, string lastName, string phoneNumber, string email, string address, List<int> trainerIds, List<int> quantities)
        {
            var client = new ClientDetails
            {
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Email = email,
                Address = address
            };
            if (!Regex.IsMatch(firstName, @"^[a-zA-Zа-яА-ЯёЁіІїЇєЄґҐ']+$") ||
                !Regex.IsMatch(lastName, @"^[a-zA-Zа-яА-ЯёЁіІїЇєЄґҐ']+$") ||
                !Regex.IsMatch(phoneNumber, @"^\d{10}$") ||
                !Regex.IsMatch(address, @"^(вул\.|просп\.)"))
            {
                ModelState.AddModelError("", "Некоректні дані. Перевірте ім'я, фамілію, телефон чи адресу.");
                ViewBag.Trainers = _context.Trainers.ToList();
                return View();
            }
            _context.ClientDetails.Add(client);
            _context.SaveChanges();

            var order = new Order
            {
                ManagerId = _context.Users.FirstOrDefault(u => u.Username == User.Identity.Name)?.Id ?? 0,
                ClientDetailsId = client.Id,
                OrderDate = DateTime.Now,
                Status = "Новый"
            };
            _context.Orders.Add(order);
            _context.SaveChanges();

            for (int i = 0; i < trainerIds.Count; i++)
            {
                var trainerId = trainerIds[i];
                var quantity = quantities[i];

                var trainer = _context.Trainers.FirstOrDefault(t => t.Id == trainerId);
                if (trainer == null || trainer.Quantity < quantity)
                {
                    ModelState.AddModelError("", $"Недостатня кількість для тренажера {trainer?.Name ?? trainerId.ToString()}.");
                    ViewBag.Trainers = _context.Trainers.ToList();
                    return View();
                }

                trainer.Quantity -= quantity;

                var inventory = _context.Inventory.FirstOrDefault(i => i.TrainerId == trainerId);
                if (inventory != null)
                {
                    inventory.Quantity -= quantity;
                    _context.Inventory.Update(inventory);
                }

                _context.Trainers.Update(trainer);

                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.Id,
                    TrainerId = trainerId,
                    Quantity = quantity,
                    Price = trainer.Price
                });
            }

            _context.SaveChanges();
            return RedirectToAction("Details", new { id = order.Id });
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var order = _context.Orders
                .Include(o => o.ClientDetails)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Trainer)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.Trainers = _context.Trainers.ToList();
            return View(order);
        }

        [HttpPost]
        public IActionResult Edit(Order order, List<int> trainerIds, List<int> quantities)
        {
            var existingOrder = _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.Id == order.Id);

            if (existingOrder == null)
            {
                return NotFound();
            }

            if (existingOrder.Status != "Завершено")
            {
                foreach (var detail in existingOrder.OrderDetails)
                {
                    var trainer = _context.Trainers.FirstOrDefault(t => t.Id == detail.TrainerId);
                    if (trainer != null)
                    {
                        trainer.Quantity += detail.Quantity;
                    }

                    var inventory = _context.Inventory.FirstOrDefault(i => i.TrainerId == detail.TrainerId);
                    if (inventory != null)
                    {
                        inventory.Quantity += detail.Quantity;
                    }
                }
            }

            _context.OrderDetails.RemoveRange(existingOrder.OrderDetails);

            var client = _context.ClientDetails.FirstOrDefault(c => c.Id == order.ClientDetailsId);
            if (client != null)
            {
                client.FirstName = order.ClientDetails.FirstName;
                client.LastName = order.ClientDetails.LastName;
                client.PhoneNumber = order.ClientDetails.PhoneNumber;
                client.Email = order.ClientDetails.Email;
                client.Address = order.ClientDetails.Address;
            }

            for (int i = 0; i < trainerIds.Count; i++)
            {
                var trainerId = trainerIds[i];
                var quantity = quantities[i];

                var trainer = _context.Trainers.FirstOrDefault(t => t.Id == trainerId);
                if (trainer == null || trainer.Quantity < quantity)
                {
                    ModelState.AddModelError("", $"Недостатня кількість для тренажера {trainer?.Name ?? trainerId.ToString()}.");
                    ViewBag.Trainers = _context.Trainers.ToList();
                    return View(order);
                }

                trainer.Quantity -= quantity;

                var inventory = _context.Inventory.FirstOrDefault(i => i.TrainerId == trainerId);
                if (inventory != null)
                {
                    inventory.Quantity -= quantity;
                }

                _context.Trainers.Update(trainer);

                existingOrder.OrderDetails.Add(new OrderDetail
                {
                    TrainerId = trainerId,
                    Quantity = quantity,
                    Price = trainer.Price
                });
            }

            existingOrder.Status = order.Status;
            _context.SaveChanges();
            return RedirectToAction("Details", new { id = order.Id });
        }
        [HttpGet]
        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.ClientDetails)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Trainer)
                .Include(o => o.Manager)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status != "Завершено")
            {
                foreach (var detail in order.OrderDetails)
                {
                    var trainer = _context.Trainers.FirstOrDefault(t => t.Id == detail.TrainerId);
                    if (trainer != null)
                    {
                        trainer.Quantity += detail.Quantity;
                    }

                    var inventory = _context.Inventory.FirstOrDefault(i => i.TrainerId == detail.TrainerId);
                    if (inventory != null)
                    {
                        inventory.Quantity += detail.Quantity;
                    }
                }
            }

            _context.OrderDetails.RemoveRange(order.OrderDetails);
            _context.Orders.Remove(order);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ConfirmDelete(int id)
        {
            var order = _context.Orders
                .Include(o => o.ClientDetails)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet]
        public IActionResult Report(string reportType)
        {
            ViewBag.ReportType = reportType;

            switch (reportType)
            {
                case "clients":
                    var clientReport = _context.Orders
                        .Include(o => o.ClientDetails)
                        .GroupBy(o => new
                        {
                            o.ClientDetails.FirstName,
                            o.ClientDetails.LastName,
                            o.ClientDetails.Email,
                            o.ClientDetails.PhoneNumber,
                            o.ClientDetails.Address
                        })
                        .Select(g => new
                        {
                            ClientName = g.Key.FirstName + " " + g.Key.LastName,
                            Email = g.Key.Email,
                            PhoneNumber = g.Key.PhoneNumber,
                            Address = g.Key.Address,
                            TotalOrders = g.Count(),
                            TotalSpent = g.Sum(o => o.OrderDetails.Sum(od => od.Quantity * od.Price))
                        })
                        .ToList();

                    return View(clientReport);

                case "profits":
                    var detailedProfitReport = _context.OrderDetails
                        .Include(od => od.Trainer)
                        .GroupBy(od => new { od.Trainer.Name, od.Trainer.Price })
                        .Select(g => new
                        {
                            TrainerName = g.Key.Name,
                            UnitPrice = g.Key.Price,
                            TotalSold = g.Sum(od => od.Quantity),
                            TotalRevenue = g.Sum(od => od.Quantity * od.Price),
                            Status = string.Empty,
                        })
                        .ToList();

                    return View(detailedProfitReport);

                case "realized":
                    var realizedReport = _context.Orders
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Trainer)
                        .Where(o => o.Status == "Завершен")
                        .Select(o => new
                        {
                            OrderId = o.Id,
                            Client = $"{o.ClientDetails.FirstName} {o.ClientDetails.LastName}",
                            TotalTrainers = o.OrderDetails.Sum(d => d.Quantity),
                            TotalPrice = o.OrderDetails.Sum(d => d.Quantity * d.Price),
                            TrainerImages = o.OrderDetails.Select(d => d.Trainer.ImagePath).ToList(),
                            Status = o.Status
                        })
                        .ToList();

                    var totalRevenue = realizedReport.Sum(r => r.TotalPrice);

                    ViewBag.TotalRevenue = totalRevenue;
                    return View(realizedReport);

                case "popular":
                    var popularTrainer = _context.OrderDetails
                        .Include(od => od.Trainer)
                        .GroupBy(od => new { od.Trainer.Name, od.Trainer.ImagePath })
                        .OrderByDescending(g => g.Sum(d => d.Quantity))
                        .Select(g => new
                        {
                            TrainerName = g.Key.Name,
                            TrainerImage = g.Key.ImagePath,
                            TotalOrdered = g.Sum(d => d.Quantity)
                        })
                        .FirstOrDefault();

                    return View(popularTrainer);

                default:
                    var summaryReport = _context.Orders
                        .GroupBy(o => o.Status)
                        .Select(g => new
                        {
                            Status = g.Key,
                            TotalOrders = g.Count(),
                            TotalRevenue = g.Sum(o => o.OrderDetails.Sum(d => d.Quantity * d.Price))
                        })
                        .ToList();
                    return View(summaryReport);
            }
        }
        [HttpPost]
        public IActionResult GeneratePdfReport()
        {
            var orders = _context.Orders
                .Include(o => o.ClientDetails)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Trainer)
                .Where(o => o.Status == "Завершен")
                .ToList();

            var clients = _context.Orders
                       .Include(o => o.ClientDetails)
                       .GroupBy(o => new
                       {
                           o.ClientDetails.FirstName,
                           o.ClientDetails.LastName,
                           o.ClientDetails.Email,
                           o.ClientDetails.PhoneNumber,
                           o.ClientDetails.Address
                       })
                       .Select(g => new
                       {
                           ClientName = g.Key.FirstName + " " + g.Key.LastName,
                           Email = g.Key.Email,
                           PhoneNumber = g.Key.PhoneNumber,
                           Address = g.Key.Address,
                           TotalOrders = g.Count(),
                           TotalSpent = g.Sum(o => o.OrderDetails.Sum(od => od.Quantity * od.Price))
                       })
                       .ToList();

            if (!orders.Any())
            {
                return Content("Немає завершених заказів для звіту.");
            }

            using (var memoryStream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                writer.CloseStream = false;

                var fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/arial.ttf");
                var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                var titleFont = new Font(baseFont, 18, Font.BOLD);
                var sectionFont = new Font(baseFont, 14, Font.BOLD);
                var bodyFont = new Font(baseFont, 12);

                document.Open();

                var title = new Paragraph("Деталізований звіт по заказам", titleFont) { Alignment = Element.ALIGN_CENTER };
                document.Add(title);
                document.Add(new Paragraph("\n\n"));

                var ordersTable = new PdfPTable(6) { WidthPercentage = 100, SpacingBefore = 10f, SpacingAfter = 10f };
                ordersTable.AddCell(new Phrase("ID Заказа", bodyFont));
                ordersTable.AddCell(new Phrase("Клієнт", bodyFont));
                ordersTable.AddCell(new Phrase("Назва тренажеру", bodyFont));
                ordersTable.AddCell(new Phrase("Продана кількість", bodyFont));
                ordersTable.AddCell(new Phrase("Сума продаж", bodyFont));
                ordersTable.AddCell(new Phrase("Чистий дохід", bodyFont));

                decimal totalNetProfit = 0;
                foreach (var order in orders)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var trainer = detail.Trainer;
                        if (trainer == null) continue;

                        var totalRevenue = detail.Quantity * trainer.Price;
                        var totalCost = detail.Quantity * trainer.ProductionCost;
                        var netProfit = totalRevenue - totalCost;
                        totalNetProfit += netProfit;

                        ordersTable.AddCell(new Phrase(order.Id.ToString(), bodyFont));
                        ordersTable.AddCell(new Phrase($"{order.ClientDetails.FirstName} {order.ClientDetails.LastName}", bodyFont));
                        ordersTable.AddCell(new Phrase(trainer.Name, bodyFont));
                        ordersTable.AddCell(new Phrase(detail.Quantity.ToString(), bodyFont));
                        ordersTable.AddCell(new Phrase(totalRevenue.ToString("C"), bodyFont));
                        ordersTable.AddCell(new Phrase(netProfit.ToString("C"), bodyFont));
                    }
                }
                document.Add(ordersTable);

                document.Add(new Paragraph("\n"));
                var netProfitParagraph = new Paragraph($"Сумарний чистий дохід: {totalNetProfit:C}", sectionFont)
                {
                    Alignment = Element.ALIGN_RIGHT
                };
                document.Add(netProfitParagraph);

                document.Add(new Paragraph("\n"));
                var clientsTitle = new Paragraph("Звіт по клієнтам", sectionFont);
                document.Add(clientsTitle);

                var clientsTable = new PdfPTable(6) { WidthPercentage = 100, SpacingBefore = 10f, SpacingAfter = 10f };
                clientsTable.AddCell(new Phrase("Клієнт", bodyFont));
                clientsTable.AddCell(new Phrase("Email", bodyFont));
                clientsTable.AddCell(new Phrase("Телефон", bodyFont));
                clientsTable.AddCell(new Phrase("Адреса", bodyFont));
                clientsTable.AddCell(new Phrase("Всього замовлень", bodyFont));
                clientsTable.AddCell(new Phrase("Загальна сума", bodyFont));

                foreach (var client in clients)
                {
                    clientsTable.AddCell(new Phrase(client.ClientName, bodyFont));
                    clientsTable.AddCell(new Phrase(client.Email ?? "-", bodyFont));
                    clientsTable.AddCell(new Phrase(client.PhoneNumber, bodyFont));
                    clientsTable.AddCell(new Phrase(client.Address ?? "-", bodyFont));
                    clientsTable.AddCell(new Phrase(client.TotalOrders.ToString(), bodyFont));
                    clientsTable.AddCell(new Phrase(client.TotalSpent.ToString("C"), bodyFont));
                }
                document.Add(clientsTable);

                document.Add(new Paragraph("\n"));
                var trainerProfits = _context.OrderDetails
                    .Include(od => od.Trainer)
                    .GroupBy(od => new { od.Trainer.Name, od.Trainer.Price, od.Trainer.ProductionCost })
                    .Select(g => new
                    {
                        TrainerName = g.Key.Name,
                        TotalQuantity = g.Sum(od => od.Quantity),
                        TotalSales = g.Sum(od => od.Quantity * g.Key.Price),
                        TotalProfit = g.Sum(od => (od.Quantity * g.Key.Price) - (od.Quantity * g.Key.ProductionCost))
                    })
                    .ToList();

                document.Add(new Paragraph("\n"));
                var profitTitle = new Paragraph("Розділ: потенційний дохід", sectionFont);
                document.Add(profitTitle);

                var profitTable = new PdfPTable(4) { WidthPercentage = 100, SpacingBefore = 10f, SpacingAfter = 10f };
                profitTable.AddCell(new Phrase("Назва тренажеру", bodyFont));
                profitTable.AddCell(new Phrase("Кількість", bodyFont));
                profitTable.AddCell(new Phrase("Сума продажі", bodyFont));
                profitTable.AddCell(new Phrase("Чистий дохід", bodyFont));

                foreach (var trainer in trainerProfits)
                {
                    profitTable.AddCell(new Phrase(trainer.TrainerName, bodyFont));
                    profitTable.AddCell(new Phrase(trainer.TotalQuantity.ToString(), bodyFont));
                    profitTable.AddCell(new Phrase(trainer.TotalSales.ToString("C"), bodyFont));
                    profitTable.AddCell(new Phrase(trainer.TotalProfit.ToString("C"), bodyFont));
                }

                document.Add(profitTable);
                var totalProfit = trainerProfits.Sum(t => t.TotalProfit);
                var totalProfitParagraph = new Paragraph($"Сумарний чистий дохід: {totalProfit:C}", sectionFont)
                {
                    Alignment = Element.ALIGN_RIGHT
                };
                document.Add(totalProfitParagraph);



                document.Close();
                memoryStream.Position = 0;

                return File(memoryStream.ToArray(), "application/pdf", "Detailed_Report.pdf");
            }
        }

    }
}
