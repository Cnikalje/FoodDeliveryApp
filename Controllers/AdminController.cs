using FoodDeliveryApp.Data;
using FoodDeliveryApp.Models;
using FoodDeliveryApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;



namespace FoodDeliveryApp.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admin role can access
    
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Products


        public IActionResult CustomerList(string searchName, string city)
        {
            var customers = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                customers = customers.Where(c => c.CustomerName.ToLower().Contains(searchName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(city) && city != "All")
            {
                customers = customers.Where(c => c.City == city);
            }

            var cityList = _context.Customers.Select(c => c.City).Distinct().OrderBy(c => c).ToList();
            cityList.Insert(0, "All");

            ViewBag.CityList = new SelectList(cityList, city);

            return View(customers.ToList());
        }



        // GET: Admin/DeleteCustomer
        public IActionResult DeleteCustomer(int customerId)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Admin/DeleteCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCustomerConfirmed(int customerId)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                _context.SaveChanges();
            }

            return RedirectToAction("CustomerList");
        }


        public IActionResult Products(string search, int page = 1)
        {
            var items = _context.Items.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                items = items.Where(i => i.ItemName.Contains(search));
            }

            ViewData["Search"] = search;
            ViewData["Filter"] = "all";

            return View("Products", items.ToPagedList(page, 5));
        }
        public IActionResult Create()
        {
          

            return View();
        }
        [HttpPost]
        public ActionResult Create(Item stud)
        {
            if (ModelState.IsValid == true)
            {
                _context.Items.Add(stud);
                int a = _context.SaveChanges();
                if (a > 0)
                {
                    TempData["InsertMassage"] = "Data inserted";
                    return RedirectToAction("Products","Admin");
                }
                else
                {
                    TempData["InsertMassage"] = "Data not inserted";
                }
            }
            return View();
        }

        public IActionResult Edit(int ItemId)
        {
            var item = _context.Items.FirstOrDefault(a => a.ItemId == ItemId);

            return View(item);
        }
        [HttpPost]

        public IActionResult Edit(Item i)
        {
            if(ModelState.IsValid == true)
            {
                _context.Entry(i).State=EntityState.Modified;
                int a =_context.SaveChanges();
                if(a > 0)
                {
                    TempData["InsertMassage"] = "Data Updated";
                    return RedirectToAction("Products", "Admin");
                }
                else
                {
                    TempData["InsertMassage"] = "Data not Updated";
                }
            }
            return View();
        }

        public IActionResult Details(int ItemId)
        {
            var item = _context.Items.FirstOrDefault(a => a.ItemId == ItemId);

            return View(item);
        }
        
        //public IActionResult Delete(int itemId)
        //{
        //    var Item= _context.Items.FirstOrDefault(a=>a.ItemId == itemId);
        //    return View(Item);
        //}
        //[HttpPost]

        //public IActionResult Delete(Item i)
        //{
        //    if(ModelState.IsValid == true)
        //    {
        //        _context.Entry(i).State = EntityState.Deleted;
        //        int a = _context.SaveChanges();
        //        if( a > 0)
        //        {
        //            TempData["Massage"] = "Data deleted Successfully";
        //            return RedirectToAction("Products", "Admin");
        //        }
        //        else
        //        {
        //            TempData["Massage"] = "Data not  deleted Successfully";
        //        }
        //    }
        //    return View();
        //}
           public ActionResult Delete(int itemId)
        {
            if (itemId > 0)
            {
                var StudentIdrow = _context.Items.Where(model => model.ItemId == itemId).FirstOrDefault();
                if (StudentIdrow != null)
                {
                    _context.Entry(StudentIdrow).State = EntityState.Deleted;
                    int a = _context.SaveChanges();
                    if (a > 0)
                    {
                        TempData["DeleteMessage"] = "<script>alert('deleted')</script>";

                    }
                    else
                    {
                        TempData["DeleteMessage"] = "<script>alert('deleted')</script>";
                    }
                }
            }
            return RedirectToAction("Products", "Admin");
        }


        // GET: /Admin/Orders
        public IActionResult Orders()
        {
            // If you need the admin ID:
            // var adminIdClaim = User.FindFirst("AdminId");
            // int adminId = adminIdClaim != null ? int.Parse(adminIdClaim.Value) : 0;

            // AutoRejectOldPendingOrders();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Fetch only today's orders AsNoTracking
            var ordersToday = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
                .Where(o => o.OrderDateTime >= today && o.OrderDateTime < tomorrow)
                .OrderByDescending(o => o.OrderDateTime)
                .ToList();

            var pendingOrders = ordersToday.Where(o => o.Status == OrderStatus.Pending);
            var acceptedOrders = ordersToday.Where(o => o.Status == OrderStatus.Accepted);
            var rejectedOrders = ordersToday.Where(o => o.Status == OrderStatus.Rejected);

            // Convert to ViewModels
            ViewBag.PendingOrders = MapToAdminOrdersViewModel(pendingOrders);
            ViewBag.AcceptedOrders = MapToAdminOrdersViewModel(acceptedOrders);
            ViewBag.RejectedOrders = MapToAdminOrdersViewModel(rejectedOrders);

            return View();
        }

        // POST: /Admin/AcceptOrder
        [HttpPost]
        public IActionResult AcceptOrder(int orderId)
        {
            // No more session checks - [Authorize(Roles="Admin")] ensures only admins can reach here
            var order = _context.Orders.Find(orderId);
            if (order != null && order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Accepted;
                _context.SaveChanges();
            }
            return RedirectToAction("Orders");
        }

        // POST: /Admin/RejectOrder
        [HttpPost]
        public IActionResult RejectOrder(int orderId)
        {
            var order = _context.Orders.Find(orderId);
            if (order != null && order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Rejected;
                _context.SaveChanges();
            }
            return RedirectToAction("Orders");
        }

        private void AutoRejectOldPendingOrders()
        {
            var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);

            var oldPendingOrders = _context.Orders
                .Where(o => o.Status == OrderStatus.Pending && o.OrderDateTime < fiveMinutesAgo)
                .ToList();

            foreach (var ord in oldPendingOrders)
            {
                ord.Status = OrderStatus.Rejected;
            }
            _context.SaveChanges();
        }

        // Maps Orders to AdminOrdersViewModel
        private List<AdminOrdersViewModel> MapToAdminOrdersViewModel(IEnumerable<Order> orders)
        {
            var result = new List<AdminOrdersViewModel>();
            foreach (var o in orders)
            {
                var itemsList = o.OrderItems.Select(oi =>
                    $"{oi.Item.ItemName} x{oi.Quantity} (${oi.UnitPrice})").ToList();

                result.Add(new AdminOrdersViewModel
                {
                    OrderId = o.OrderId,
                    CustomerName = o.Customer.CustomerName,
                    CustomerEmail = o.Customer.Email,
                    Items = itemsList,
                    TotalPrice = o.Price,
                    OrderDateTime = o.OrderDateTime.ToString("g")
                });
            }
            return result;
        }

        public IActionResult AvailableItems(string search, int page = 1)
        {
            ViewData["Filter"] = "available";
            var items = _context.Items.Where(i => i.IsAvailable).ToList();
            

            return View("Products", items.ToPagedList(page, 5));
        }

        //public IActionResult AvailableItems()
        //{

        //    ViewData["Filter"] = "available";
        //    var items = _context.Items.Where(i => i.IsAvailable).ToList();
        //    return View("Products", items); // Reuse Products view
        //}

        public IActionResult UnavailableItems(string search, int page = 1)
        {
            ViewData["Filter"] = "unavailable";
            var items = _context.Items.Where(i => !i.IsAvailable).ToList();


            return View("Products", items.ToPagedList(page, 5));
        }
        //public IActionResult UnavailableItems()
        //{
        //    ViewData["Filter"] = "unavailable";
        //    var items = _context.Items.Where(i => !i.IsAvailable).ToList();
        //    return View("Products", items); // Reuse Products view
        //}
    }
}