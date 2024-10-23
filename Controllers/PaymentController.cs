using Microsoft.AspNetCore.Mvc;
using Mailo.Models;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Mailo.Data;
using Mailo.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
	public class PaymentController : Controller
	{
		private string PaypalClientId { get; set; }
		private string PaypalSecret { get; set; }
		private string PaypalUrl { get; set; }

		public PaymentController(IConfiguration configuration)
		{
			PaypalClientId = configuration["PaypalSettings:ClientId"]!;

			PaypalSecret = configuration["PaypalSettings:Secret"]!;
			PaypalUrl = configuration["PaypalSettings:Url"]!;

		}
		public IActionResult Index()
		{
			ViewBag.PaypalClientId = PaypalClientId;
			return View();
		}

		[HttpPost]
		public async Task<JsonResult> CreateOrder([FromBody] JsonObject data)
		{
			var totalAmount = data?["amount"]?.ToString();

			if (totalAmount == null)
			{
				return new JsonResult(new { Id = "" });
			}

			// إنشاء الطلب وحفظه في قاعدة البيانات
			var order = new Order
			{
				OrderDate = DateTime.Now,
				TotalPrice = decimal.Parse(totalAmount),
				OrderStatus = OrderStatus.Pending
			};

			using (var dbContext = new AppDbContext())
			{
				dbContext.Orders.Add(order);
				await dbContext.SaveChangesAsync();
			}

			// إعداد الطلب لـ PayPal
			JsonObject createOrderRequest = new JsonObject();
			createOrderRequest.Add("intent", "CAPTURE");

			JsonObject amount = new JsonObject();
			amount.Add("currency_code", "EGP");
			amount.Add("value", totalAmount);

			JsonObject purchaseUnit1 = new JsonObject();
			purchaseUnit1.Add("amount", amount);

			JsonArray purchaseUnits = new JsonArray();
			purchaseUnits.Add(purchaseUnit1);

			createOrderRequest.Add("purchase_units", purchaseUnits);

			string accessToken = await GetPaypalAccessToken();
			string url = PaypalUrl + "/v2/checkout/orders";

			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
				var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
				requestMessage.Content = new StringContent(createOrderRequest.ToString(), null, "application/json");

				var httpResponse = await client.SendAsync(requestMessage);

				if (httpResponse.IsSuccessStatusCode)
				{
					var strResponse = await httpResponse.Content.ReadAsStringAsync();
					var jsonResponse = JsonNode.Parse(strResponse);

					if (jsonResponse != null)
					{
						string paypalOrderId = jsonResponse["Id"]?.ToString() ?? "";

						// تعديل حالة الطلب بعد أن يتم إنشاؤه في PayPal
						using (var dbContext = new AppDbContext())
						{
							var savedOrder = dbContext.Orders.Find(order.ID);
							if (savedOrder != null)
							{
								savedOrder.OrderStatus = OrderStatus.New;
								await dbContext.SaveChangesAsync();
							}
						}

						return new JsonResult(new { Id = paypalOrderId });
					}
				}
			}

			return new JsonResult(new { Id = "" });
		}


		[HttpPost]
		public async Task<JsonResult> CompleteOrder([FromBody] JsonObject data)
		{
			var orderId = data?["orderID"]?.ToString();

			if (orderId == null)
			{
				return new JsonResult("error");
			}

			string accessToken = await GetPaypalAccessToken();
			string url = PaypalUrl + "/v2/checkout/orders/" + orderId + "/capture";

			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
				var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
				requestMessage.Content = new StringContent("", null, "application/json");

				var httpResponse = await client.SendAsync(requestMessage);

				if (httpResponse.IsSuccessStatusCode)
				{
					var strResponse = await httpResponse.Content.ReadAsStringAsync();
					var jsonResponse = JsonNode.Parse(strResponse);

					if (jsonResponse != null)
					{
						string paypalOrderStatus = jsonResponse["status"]?.ToString() ?? "";

						if (paypalOrderStatus == "COMPLETED")
						{
							// إتمام الدفع وتسجيله في قاعدة البيانات
							using (var dbContext = new AppDbContext())
							{
								var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderStatus== OrderStatus.Delivered);
								if (order != null)
								{
									Payment payment = new Payment
									{
										OrderId = order.ID,
										PaymentDate = DateTime.Now,
										TotalPrice = order.TotalPrice,
										PaymentStatus = PaymentStatus.Paid,
										PaymentMethod = PaymentMethod.Paypal
									};
									dbContext.Payments.Add(payment);
									order.OrderStatus = OrderStatus.Delivered;
									await dbContext.SaveChangesAsync();
								}
							}

							return new JsonResult("success");
						}
					}
				}

				return new JsonResult("error");
			}
		}


		private async Task<string> GetPaypalAccessToken()
		{
			string accessToken = "";
			string url = PaypalUrl + "/v1/oauth2/token";
			using (var client = new HttpClient())
			{
				string credentials64 =
					  Convert.ToBase64String(Encoding.UTF8.GetBytes(PaypalClientId + ":" + PaypalSecret));
				client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials64);

				var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
				requestMessage.Content = new StringContent("grant_type=client_credentials", null, "application/x-www-form-urlencoded");
				var httpResponse = await client.SendAsync(requestMessage);
				if (httpResponse.IsSuccessStatusCode)
				{
					var strResponse = await httpResponse.Content.ReadAsStringAsync();
					var jsonResponse = JsonNode.Parse(strResponse);
					if (jsonResponse != null)
					{
						accessToken = jsonResponse["access_token"]?.ToString() ?? "";
					}
				}

			}

			return accessToken;
		}
	}

}