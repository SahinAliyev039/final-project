﻿using System.ComponentModel.DataAnnotations;

namespace BackendProject.Areas.Admin.ViewModels;

public class ProductViewModel
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public double Price { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    public IFormFile? Image { get; set; }
    public int CategoryId { get; set; }
}
