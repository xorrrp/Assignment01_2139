using Microsoft.AspNetCore.Mvc;
using Assignment1.Data;
using Assignment1.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Assignment1.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult CreateOrder()
        {
            var products = _context.Products.ToList();
            
            if (products == null || !products.Any())
            {
                ModelState.AddModelError("", "No products available");
                ViewData["Products"] = new List<Product>();
                return View();
            }
            ViewData["Products"] = products;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(Order model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Products"] = _context.Products.ToList();
                return View(model);
            }
                
            var order = new Order
            {
                CustomerName = model.CustomerName,
                CustomerEmail = model.CustomerEmail,
                OrderDate = DateTime.UtcNow,
                TotalPrice = 0,
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in model.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    order.OrderItems.Add(new OrderItem
                    { 
                        ProductId = product.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price * item.Quantity
                    });
                    order.TotalPrice += product.Price * item.Quantity;
                }
            }
            
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(OrderConfirmation), new { orderId = order.Id });
            
        }

        public IActionResult OrderConfirmation(int? orderId)
        {
            if (orderId == null)
            {
                return RedirectToAction("Index", "Products");
            }
            
            var order = _context.Orders
                .Include(o => o.OrderItems) 
                .ThenInclude(oi => oi.Product) 
                .FirstOrDefault(o => o.Id == orderId.Value);
            
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
