﻿using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using Razorpay.Api;
using System;

namespace Mailo.Controllers
{
    public class MyOrderController : Controller
    {
        [BindProperty]
        public EntityOrder _OrderDetails { get; set; }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CreateOrder()
        {
            string key = "rzp_test_GuoTeNjQD52B1J";
            string secret = "HE1lapQfySHaIRiI5d9QLDfS";


            Random random = new Random();
            string transactionId = random.Next(0, 3000).ToString();

            Dictionary<string, object> input = new Dictionary<string, object>();
            input.Add("amount", Convert.ToDecimal(_OrderDetails.Amount) * 100); 
            input.Add("currency", "INR");
            input.Add("receipt", "transactionId");



            RazorpayClient client = new RazorpayClient(key, secret);

            Razorpay.Api.Order order = client.Order.Create(input);
            ViewBag.orderId = order["id"].ToString();
            return View("Payment", _OrderDetails);
        }
        public ActionResult Payment(string razor_payment_id, string razorpay_order_id, string razorpay_signature)
        {
            RazorpayClient client = new RazorpayClient("[YOUR_KEY_ID]", "[YOUR_KEY_SECRET]");

            Dictionary<string, string> attributes = new Dictionary<string, string>();

            attributes.Add("razorpay_payment_id", razor_payment_id);
            attributes.Add("razorpay_order_id", razorpay_order_id);
            attributes.Add("razorpay_signature", razorpay_signature);

            Utils.verifyPaymentSignature(attributes);
            EntityOrder orderDtl = new EntityOrder();
            orderDtl.TransactionId = razorpay_order_id;
            orderDtl.OrderId = razorpay_order_id;


            return View("PaymentSucess", orderDtl);

        }
    }
}
