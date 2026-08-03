using Microsoft.EntityFrameworkCore;
using Backend.Enums;
using Backend.Models;

namespace Backend.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(MyDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var rng = new Random(42);
        var now = DateTime.UtcNow;

        string Hash(string pw) => BCrypt.Net.BCrypt.HashPassword(pw);

        var allProducts = new List<Product>();
        var allModifierGroups = new List<ModifierGroup>();
        var allModifiers = new List<Modifier>();

        (string Mod, int Price, bool IsDef) M(string name, int price, bool isDef) => (name, price, isDef);
        (string MgName, bool Required, int Max, (string Mod, int Price, bool IsDef)[] Mgs) Mg(string name, bool req, int max, params (string Mod, int Price, bool IsDef)[] mods) => (name, req, max, mods);
        (string Prod, string Desc, int Price, StockStatus Status, (string MgName, bool Required, int Max, (string Mod, int Price, bool IsDef)[] Mgs)[] Mgs) Prod(string name, string desc, int price, StockStatus status, params (string MgName, bool Required, int Max, (string Mod, int Price, bool IsDef)[] Mgs)[] mgs) => (name, desc, price, status, mgs);
        (string Name, (string Prod, string Desc, int Price, StockStatus Status, (string MgName, bool Required, int Max, (string Mod, int Price, bool IsDef)[] Mgs)[] Mgs)[] Prods) Cat(string name, params (string Prod, string Desc, int Price, StockStatus Status, (string MgName, bool Required, int Max, (string Mod, int Price, bool IsDef)[] Mgs)[] Mgs)[] prods) => (name, prods);

        void AddCategories(int canteenId, params (string Name, (string Prod, string Desc, int Price, StockStatus Status, (string MgName, bool Required, int Max, (string Mod, int Price, bool IsDef)[] Mgs)[] Mgs)[] Prods)[] cats)
        {
            int catOrder = 0;
            foreach (var cat in cats)
            {
                var category = new Category { CanteenId = canteenId, Name = cat.Name, DisplayOrder = catOrder++ };
                db.Categories.Add(category);
                db.SaveChanges();
                foreach (var prod in cat.Prods)
                {
                    var product = new Product
                    {
                        CanteenId = canteenId,
                        CategoryId = category.Id,
                        Name = prod.Prod,
                        Description = prod.Desc,
                        BasePriceAmount = (decimal)prod.Price,
                        Status = prod.Status,
                        SoldCount = 0
                    };
                    db.Products.Add(product);
                    db.SaveChanges();
                    allProducts.Add(product);
                    int mgOrder = 0;
                    foreach (var mg in prod.Mgs)
                    {
                        var mgroup = new ModifierGroup
                        {
                            ProductId = product.Id,
                            Name = mg.MgName,
                            Required = mg.Required,
                            MaxSelected = mg.Max,
                            DisplayOrder = mgOrder++,
                            Status = StockStatus.Available
                        };
                        db.ModifierGroups.Add(mgroup);
                        db.SaveChanges();
                        allModifierGroups.Add(mgroup);
                        int modOrder = 0;
                        foreach (var m in mg.Mgs)
                        {
                            db.Modifiers.Add(new Modifier
                            {
                                ModifierGroupId = mgroup.Id,
                                Name = m.Mod,
                                PriceAmount = (decimal)m.Price,
                                DisplayOrder = modOrder++,
                                IsDefault = m.IsDef,
                                Status = StockStatus.Available
                            });
                            allModifiers.Add(db.Modifiers.Local.Last());
                        }
                    }
                }
            }
        }

        var admin = new User
        {
            Username = "admin",
            Email = "admin@cantee.com",
            PasswordHash = Hash("123456"),
            FullName = "Quản Trị Viên",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now.AddDays(-45)
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var canteenDefs = new[]
        {
            new { Name = "Căn tin 1",           Addr = "Tầng 1, Tòa A, 123 Nguyễn Văn Cừ",         Phone = "0281000001", Email = "cantin1@cantee.com", StaffName = "Nguyễn Văn A" },
            new { Name = "Căn tin 2",           Addr = "Tầng trệt, Tòa B, 45 Lê Lợi",              Phone = "0281000002", Email = "cantin2@cantee.com", StaffName = "Nguyễn Văn B" },
            new { Name = "Căn tin 3",           Addr = "Lầu 2, Khu C, 78 Hai Bà Trưng",            Phone = "0281000003", Email = "cantin3@cantee.com", StaffName = "Nguyễn Văn C" },
            new { Name = "Căn tin 4",           Addr = "Sảnh chính, 22 Trần Hưng Đạo",             Phone = "0281000004", Email = "cantin4@cantee.com", StaffName = "Nguyễn Văn D" },
            new { Name = "Căn tin 5",           Addr = "Khu B, 56 Điện Biên Phủ",                  Phone = "0281000005", Email = "cantin5@cantee.com", StaffName = "Nguyễn Văn E" },
            new { Name = "Căn tin 6",           Addr = "Tòa C, 90 Hoàng Diệu",                     Phone = "0281000006", Email = "cantin6@cantee.com", StaffName = "Nguyễn Văn F" },
            new { Name = "Căn tin 7",           Addr = "Tầng lửng, 15 Nguyễn Huệ",                 Phone = "0281000007", Email = "cantin7@cantee.com", StaffName = "Nguyễn Văn G" },
            new { Name = "Căn tin 8",           Addr = "Khu thể thao, 33 Võ Văn Tần",              Phone = "0281000008", Email = "cantin8@cantee.com", StaffName = "Nguyễn Văn H" },
            new { Name = "Căn tin Đại học CMC", Addr = "Tòa nhà chính, ĐH CMC, 84C Nguyễn Thanh Bình", Phone = "0281000009", Email = "dhcmc@cantee.com", StaffName = "Nguyễn Văn I" },
            new { Name = "Căn tin THPT CMC",    Addr = "Cơ sở 2, THPT CMC, 112 Đường số 3",        Phone = "0281000010", Email = "thptcmc@cantee.com", StaffName = "Nguyễn Văn J" },
        };

        var staffList = new List<User>();
        var canteenList = new List<Canteen>();
        foreach (var (def, idx) in canteenDefs.Select((d, i) => (d, i)))
        {
            staffList.Add(new User
            {
                Username = $"staff_c{idx + 1}",
                Email = def.Email,
                PasswordHash = Hash("123456"),
                FullName = def.StaffName,
                Role = UserRole.Staff,
                IsActive = true,
                CreatedAt = now.AddDays(-40)
            });
        }
        db.Users.AddRange(staffList);
        await db.SaveChangesAsync();

        foreach (var (def, idx) in canteenDefs.Select((d, i) => (d, i)))
        {
            canteenList.Add(new Canteen
            {
                Name = def.Name,
                Address = def.Addr,
                PhoneNumber = def.Phone,
                Email = def.Email,
                StaffId = staffList[idx].Id,
                Status = CanteenStatus.Active
            });
        }
        db.Canteens.AddRange(canteenList);
        await db.SaveChangesAsync();

        var customerNames = new[] { "Nguyễn Văn An", "Trần Thị Bình", "Lê Văn Chiến", "Phạm Thị Dung", "Hoàng Văn Đức", "Đỗ Thị Hoa", "Vũ Văn Hùng", "Bùi Thị Lan", "Đặng Văn Minh", "Ngô Thị Nhung" };
        var customers = new List<User>();
        for (int i = 1; i <= 10; i++)
        {
            customers.Add(new User
            {
                Username = $"customer{i}",
                Email = $"customer{i}@email.com",
                PasswordHash = Hash("123456"),
                FullName = customerNames[i - 1],
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = now.AddDays(-30 - rng.Next(0, 15))
            });
        }
        db.Users.AddRange(customers);
        await db.SaveChangesAsync();

        var hourDefs = new[]
        {
            new { Open = new TimeOnly(7,0), Close = new TimeOnly(17,0), ClosedSun = true },
            new { Open = new TimeOnly(6,0), Close = new TimeOnly(22,0), ClosedSun = false },
            new { Open = new TimeOnly(6,30), Close = new TimeOnly(20,0), ClosedSun = true },
            new { Open = new TimeOnly(7,0), Close = new TimeOnly(21,0), ClosedSun = false },
            new { Open = new TimeOnly(8,0), Close = new TimeOnly(20,0), ClosedSun = false },
            new { Open = new TimeOnly(6,30), Close = new TimeOnly(21,0), ClosedSun = false },
            new { Open = new TimeOnly(8,0), Close = new TimeOnly(22,0), ClosedSun = false },
            new { Open = new TimeOnly(6,0), Close = new TimeOnly(18,0), ClosedSun = true },
            new { Open = new TimeOnly(10,0), Close = new TimeOnly(22,0), ClosedSun = false },
            new { Open = new TimeOnly(6,30), Close = new TimeOnly(22,30), ClosedSun = false },
        };
        foreach (var (c, idx) in canteenList.Select((c, i) => (c, i)))
        {
            var hd = hourDefs[idx];
            foreach (WeekDay day in Enum.GetValues<WeekDay>())
            {
                db.OperatingHours.Add(new OperatingHour
                {
                    CanteenId = c.Id,
                    DayOfWeek = day,
                    OpenTime = hd.Open,
                    CloseTime = hd.Close,
                    IsClosed = hd.ClosedSun && day == WeekDay.Sunday
                });
            }
        }
        await db.SaveChangesAsync();


        AddCategories(canteenList[0].Id,
            Cat("Cơm",
                Prod("Cơm tấm sườn nướng", "Cơm tấm miếng sườn heo nướng đậm đà, ăn kèm bì chả trứng, mỡ hành và đồ chua", 30000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false)),
                    Mg("Thêm món", false, 3, M("Trứng ốp la", 5000, false), M("Chả lụa", 5000, false), M("Bì heo", 5000, false))),
                Prod("Cơm gà nướng", "Đùi gà nướng mật ong, cơm trắng kèm rau sống và nước mắm chua ngọt", 32000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false))),
                Prod("Cơm thịt kho tàu", "Thịt ba chỉ kho trứng cút, nước dừa tươi, cơm trắng dẻo thơm", 28000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false)),
                    Mg("Thêm món", false, 2, M("Thêm trứng cút (2 viên)", 3000, false), M("Thêm thịt kho", 10000, false))),
                Prod("Cơm chiên dương châu", "Cơm chiên với lạp xưởng, tôm, trứng, đậu Hà Lan, cà rốt, thơm ngon đậm vị", 30000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false)))),
            Cat("Bún - Phở",
                Prod("Phở bò tái", "Bánh phở mềm, nước dùng hầm xương bò 12h, thịt bò tái mỏng, hành ngò thơm", 35000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, false), M("Vừa", 5000, true), M("Lớn", 8000, false)),
                    Mg("Thêm topping", false, 3, M("Thêm thịt bò", 12000, false), M("Thêm bò viên", 8000, false), M("Trứng chần", 5000, false))),
                Prod("Bún riêu cua", "Bún riêu đậm đà với riêu cua, đậu phụ chiên, chả cua, cà chua, ăn kèm rau sống", 30000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, false), M("Vừa", 5000, true), M("Lớn", 8000, false))),
                Prod("Mì xào bò", "Mì trứng xào thịt bò, giá đỗ, cải ngọt, hành tây, sốt đậm đà", 32000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false))),
                Prod("Bún thịt nướng", "Bún tươi ăn kèm thịt heo nướng thơm, chả giò, đậu phộng, nước mắm chua ngọt", 30000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false)))),
            Cat("Đồ uống",
                Prod("Trà đá", "Trà xanh thanh mát, đá lạnh, giải khát tức thì", 3000, StockStatus.Available),
                Prod("Cà phê đen", "Cà phê phin đậm đà, nguyên chất 100% Robusta", 12000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, true), M("Lớn", 5000, false))),
                Prod("Nước cam ép", "Nước cam tươi ép nguyên chất, không đường nhân tạo", 15000, StockStatus.Available),
                Prod("Coca Cola", "Nước ngọt Coca Cola lon 330ml", 8000, StockStatus.Available)));

        AddCategories(canteenList[1].Id,
            Cat("Cơm",
                Prod("Cơm sườn cốt lết", "Sườn cốt lết chiên vàng giòn, cơm trắng, canh rau củ, đồ chua", 30000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false))),
                Prod("Cơm bò lúc lắc", "Thịt bò xào lúc lắc mềm thơm, ăn kèm khoai tây chiên, salad rau", 38000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false))),
                Prod("Cơm cá kho tộ", "Cá basa kho tộ đậm đà cùng nước dừa, tiêu, ớt, ăn với cơm trắng", 28000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false)))),
            Cat("Bún - Phở",
                Prod("Bún bò Huế", "Bún bò Huế chính gốc, nước dùng hầm xương bò, giò heo, chả, sả ớt", 35000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, false), M("Vừa", 5000, true), M("Lớn", 8000, false)),
                    Mg("Độ cay", true, 1, M("Không cay", 0, false), M("Ít cay", 0, true), M("Vừa cay", 0, false))),
                Prod("Phở gà", "Phở gà ta vàng óng, nước dùng trong ngọt, hành lá, rau thơm", 32000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, false), M("Vừa", 5000, true), M("Lớn", 8000, false))),
                Prod("Hủ tiếu nam vang", "Hủ tiếu tôm thịt, nước dùng ngọt thanh từ xương heo, giá hẹ", 30000, StockStatus.Available),
                Prod("Mì Quảng", "Mì Quảng gà hoặc tôm thịt, đậu phộng rang, bánh tráng nướng, rau sống", 30000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Trà đá", "Trà xanh mát lạnh giải khát", 3000, StockStatus.Available),
                Prod("Cà phê sữa đá", "Cà phê phin đậm đà pha sữa đặc, uống đá hoặc nóng", 15000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, true), M("Lớn", 5000, false))),
                Prod("Trà sữa trân châu", "Trà sữa Đài Loan thơm béo, trân châu đen dai ngon", 20000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Vừa", 0, true), M("Lớn", 5000, false)),
                    Mg("Độ ngọt", true, 1, M("Ít ngọt", 0, false), M("Vừa", 0, true), M("Ngọt", 0, false))),
                Prod("Nước suối", "Nước khoáng tinh khiết 500ml", 5000, StockStatus.Available)));

        AddCategories(canteenList[2].Id,
            Cat("Bánh mì",
                Prod("Bánh mì thịt nướng", "Thịt heo nướng thơm lừng, đồ chua, pate, bơ, dưa leo, ngò, ớt", 20000, StockStatus.Available,
                    Mg("Thêm topping", false, 3, M("Thêm pate", 3000, false), M("Thêm trứng ốp la", 5000, false), M("Thêm chả lụa", 5000, false))),
                Prod("Bánh mì chả cá", "Chả cá chiên vàng, tương ớt, đồ chua, rau thơm", 22000, StockStatus.Available,
                    Mg("Thêm topping", false, 3, M("Thêm pate", 3000, false), M("Thêm trứng", 5000, false))),
                Prod("Bánh mì ốp la", "Bánh mì giòn chấm trứng ốp la, pate, bơ, hành phi", 15000, StockStatus.Available),
                Prod("Bánh mì que", "Bánh mì que giòn tan, phết pate bơ, tương ớt, dành cho ăn nhẹ", 8000, StockStatus.Available)),
            Cat("Xôi",
                Prod("Xôi mặn", "Xôi nếp dẻo, chả lụa, bì heo, trứng chiên, ruốc, hành phi", 20000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Vừa", 0, true), M("Lớn", 5000, false))),
                Prod("Xôi gà", "Xôi nếp cốm xanh, gà xé, ruốc, hành phi thơm", 22000, StockStatus.Available),
                Prod("Xôi ngọt", "Xôi nếp dẻo rắc đường, dừa nạo, vừng rang hoặc đậu phộng", 12000, StockStatus.Available)),
            Cat("Ăn vặt",
                Prod("Bánh bao nhân thịt", "Bánh bao trắng mềm, nhân thịt heo, trứng cút, mộc nhĩ", 12000, StockStatus.Available),
                Prod("Bánh giò", "Bánh giò nóng hổi, nhân thịt, mộc nhĩ, lá chuối xanh", 10000, StockStatus.Available),
                Prod("Chả giò chiên", "Chả giò giòn rụm, nhân thịt tôm, ăn kèm tương ớt (3 cuốn)", 15000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Trà đá", "Trà xanh mát lạnh", 3000, StockStatus.Available),
                Prod("Nước ngọt", "Nước ngọt các loại (Coca, Pepsi, 7Up, Mirinda)", 8000, StockStatus.Available),
                Prod("Sữa tươi", "Sữa tươi tiệt trùng có đường 220ml", 10000, StockStatus.Available)));

        AddCategories(canteenList[3].Id,
            Cat("Cơm",
                Prod("Cơm gà xối mỡ", "Gà chiên xối mỡ da giòn, cơm trắng, rau răm, nước mắm gừng", 35000, StockStatus.Available,
                    Mg("Phần", true, 1, M("Đùi", 0, true), M("Ức", 0, false), M("Cả con nhỏ", 20000, false))),
                Prod("Cơm rang hải sản", "Cơm rang với mực, tôm, trứng, cà rốt, đậu que, sốt dầu hào", 32000, StockStatus.Available),
                Prod("Cơm đùi gà sốt", "Đùi gà sốt tiêu đen hoặc sốt BBQ, cơm trắng, rau luộc", 32000, StockStatus.Available,
                    Mg("Sốt", true, 1, M("Tiêu đen", 0, true), M("BBQ", 0, false), M("Chanh mật ong", 0, false)))),
            Cat("Mì",
                Prod("Mì xào hải sản", "Mì trứng xào tôm, mực, cải xanh, cà rốt, sốt dầu hào thơm", 35000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false))),
                Prod("Mì khô hải sản", "Mì trứng khô trộn sốt tương đen, tôm, mực, rau cải", 30000, StockStatus.Available),
                Prod("Mì gói xào bò", "Mì gói xào thịt bò, giá đỗ, cải ngọt, trứng ốp la", 25000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Trà đá", "Trà xanh mát lạnh", 3000, StockStatus.Available),
                Prod("Nước cam ép", "Cam tươi vắt nguyên chất", 15000, StockStatus.Available),
                Prod("Sinh tố xoài", "Sinh tố xoài chín ngọt, sữa đặc, đá bào", 20000, StockStatus.Available)));

        AddCategories(canteenList[4].Id,
            Cat("Cơm văn phòng",
                Prod("Cơm bò xào tiêu đen", "Thịt bò Úc xào tiêu đen, sốt nấm, cơm trắng, rau củ luộc", 42000, StockStatus.Available),
                Prod("Cơm cá hồi sốt bơ", "Cá hồi Na Uy áp chảo sốt bơ chanh, cơm trắng, măng tây", 45000, StockStatus.Available),
                Prod("Cơm sườn nướng BBQ", "Sườn heo Mỹ nướng sốt BBQ, khoai tây nghiền, salad", 38000, StockStatus.Available),
                Prod("Cơm gà teriyaki", "Gà sốt teriyaki Nhật Bản, cơm trắng, bông cải xanh", 35000, StockStatus.Available)),
            Cat("Salad - Gỏi",
                Prod("Salad gà Caesar", "Xà lách Roma, gà nướng, phô mai Parmesan, bánh mì nướng, sốt Caesar", 28000, StockStatus.Available),
                Prod("Gỏi cuốn tôm thịt", "Bánh tráng cuốn tôm, thịt heo, bún, rau sống, chấm tương đậu phộng (3 cuốn)", 25000, StockStatus.Available),
                Prod("Gỏi xoài khô bò", "Xoài xanh bào sợi, khô bò, rau thơm, đậu phộng, nước mắm chua ngọt", 25000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Cà phê sữa đá", "Cà phê phin đậm đà pha sữa đặc", 15000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, true), M("Lớn", 5000, false))),
                Prod("Trà đào cam sả", "Trà ướp hương đào, cam tươi, sả, mật ong, thơm mát", 22000, StockStatus.Available),
                Prod("Nước ép dưa hấu", "Dưa hấu tươi ép nguyên chất", 18000, StockStatus.Available),
                Prod("Latte nóng", "Cà phê espresso pha sữa tươi nóng, lớp bọt mịn", 25000, StockStatus.Available)));

        AddCategories(canteenList[5].Id,
            Cat("Cơm",
                Prod("Cơm tấm đặc biệt", "Cơm tấm sườn, bì, chả, trứng, chà bông, mỡ hành, đồ chua đầy đủ", 38000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Tiêu chuẩn", 0, true), M("Lớn", 7000, false))),
                Prod("Cơm thịt nướng muối ớt", "Thịt heo nướng muối ớt Tây Ninh, cơm trắng, rau sống", 28000, StockStatus.Available),
                Prod("Cơm cá diêu hồng chiên", "Cá diêu hồng chiên giòn, cơm trắng, nước mắm me", 30000, StockStatus.OutOfStock)),
            Cat("Bún - Phở",
                Prod("Bún mọc", "Bún tươi ăn kèm mọc heo, sườn non, hành ngò, nước dùng trong ngọt", 30000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, false), M("Vừa", 5000, true))),
                Prod("Bún chả Hà Nội", "Bún tươi, chả miếng nướng, chả băm, nước mắm chua ngọt, rau sống", 30000, StockStatus.Available),
                Prod("Phở xào bò", "Bánh phở tươi xào thịt bò, cải ngọt, giá đỗ, sốt xào đậm đà", 32000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Trà đá", "Trà xanh mát lạnh giải khát", 3000, StockStatus.Available),
                Prod("Cà phê đen đá", "Cà phê phin không đường, vị đắng đậm đà", 12000, StockStatus.Available),
                Prod("Trà sữa matcha", "Matcha Nhật Bản pha sữa tươi, trân châu đen", 25000, StockStatus.Available)));

        AddCategories(canteenList[6].Id,
            Cat("Chè - Tráng miệng",
                Prod("Chè đậu đỏ", "Đậu đỏ hầm nhừ, nước cốt dừa béo, đá bào", 15000, StockStatus.Available,
                    Mg("Độ ngọt", true, 1, M("Ít ngọt", 0, false), M("Vừa", 0, true), M("Ngọt", 0, false))),
                Prod("Chè Thái sầu riêng", "Chè Thái đầy đủ: sầu riêng, mít, nhãn, thạch, trân châu, nước cốt dừa", 25000, StockStatus.Available),
                Prod("Chè bưởi", "Cùi bưởi giòn sần sật, nước đường thanh mát, đậu xanh, nước cốt dừa", 18000, StockStatus.Available),
                Prod("Sữa chua nếp cẩm", "Sữa chua chua ngọt hòa quyện nếp cẩm dẻo thơm, đá bào", 15000, StockStatus.Available)),
            Cat("Bánh",
                Prod("Bánh flan", "Bánh flan caramel mềm mịn, béo ngậy vị trứng sữa", 10000, StockStatus.Available),
                Prod("Bánh tiramisu", "Bánh tiramisu Ý chuẩn vị, cà phê, mascarpone, bột cacao", 28000, StockStatus.Available),
                Prod("Bánh mousse chanh dây", "Bánh mousse chanh dây chua nhẹ, lớp đế bánh giòn, mát lạnh", 25000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Trà thảo mộc", "Trà hoa cúc, atiso, cam thảo thanh nhiệt giải độc", 12000, StockStatus.Available),
                Prod("Nước sâm bí đao", "Nước sâm bí đao mát lành, la hán quả, rễ tranh", 10000, StockStatus.Available),
                Prod("Sữa hạt điều", "Sữa hạt điều rang thơm, không đường, bổ dưỡng", 18000, StockStatus.Available)));

        AddCategories(canteenList[7].Id,
            Cat("Cơm",
                Prod("Cơm chay", "Cơm trắng, đậu phụ sốt cà, nấm xào, rau củ luộc, canh rong biển", 25000, StockStatus.Available),
                Prod("Cơm trứng chiên", "Trứng chiên vàng rộm, cơm trắng, rau luộc, nước tương", 18000, StockStatus.Available),
                Prod("Cơm sườn chay", "Sườn non chay làm từ đậu nành, cơm trắng, rau củ", 22000, StockStatus.Available)),
            Cat("Bún - Phở",
                Prod("Bún riêu chay", "Bún riêu đậu phụ, nấm, cà chua, rau sống, đậm đà hương vị", 25000, StockStatus.Available),
                Prod("Phở chay", "Phở nước dùng rau củ, nấm hương, đậu phụ chiên, rau thơm", 25000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Trà đá", "Trà xanh mát lạnh", 3000, StockStatus.Available),
                Prod("Nước ép cà rốt", "Cà rốt tươi ép nguyên chất, bổ sung vitamin A", 15000, StockStatus.Available),
                Prod("Sữa đậu nành", "Sữa đậu nành nóng hoặc lạnh, thơm béo tự nhiên", 10000, StockStatus.Available)));

        AddCategories(canteenList[8].Id,
            Cat("Cơm",
                Prod("Cơm tấm đại học", "Cơm tấm sườn nướng, bì, chả, trứng, đầy đủ topping, giá sinh viên", 30000, StockStatus.Available,
                    Mg("Suất", true, 1, M("Vừa", 0, true), M("Lớn", 7000, false))),
                Prod("Cơm gà kho gừng", "Cơm gà kho gừng sả ớt đậm đà, canh rau củ", 28000, StockStatus.Available),
                Prod("Cơm rang bò", "Cơm rang thịt bò, trứng, cà rốt, đậu Hà Lan, sốt dầu hào", 28000, StockStatus.Available),
                Prod("Cơm cá sốt cà", "Cá phi lê sốt cà chua, cơm trắng, rau luộc", 25000, StockStatus.Available)),
            Cat("Bún - Phở",
                Prod("Phở bò", "Phở bò tái nạm gầu, nước dùng hầm xương, hành ngò thơm", 32000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Vừa", 0, true), M("Lớn", 8000, false))),
                Prod("Bún đậu mắm tôm", "Bún lá, đậu phụ chiên, chả cốm, thịt luộc, dồi, mắm tôm chanh ớt", 30000, StockStatus.Available),
                Prod("Bánh canh cua", "Bánh canh bột lọc sợi to, nước dùng cua thịt, chả cá, hành tiêu", 28000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Cà phê sữa đá", "Cà phê phin sữa đặc, thức uống quốc dân", 15000, StockStatus.Available,
                    Mg("Kích cỡ", true, 1, M("Nhỏ", 0, true), M("Lớn", 5000, false))),
                Prod("Trà sữa trân châu", "Trà sữa Đài Loan, trân châu đen, đường đen", 20000, StockStatus.Available,
                    Mg("Độ ngọt", true, 1, M("Ít ngọt", 0, false), M("Vừa", 0, true), M("Ngọt", 0, false))),
                Prod("Nước cam ép", "Cam tươi vắt nguyên chất, giàu vitamin C", 15000, StockStatus.Available),
                Prod("Nước suối", "Nước khoáng tinh khiết 500ml", 5000, StockStatus.Available),
                Prod("Trà đá", "Trà xanh thanh mát, miễn phí khi mua cơm", 3000, StockStatus.Available)),
            Cat("Ăn vặt",
                Prod("Bánh tráng trộn", "Bánh tráng trộn muối tôm, xoài, trứng cút, khô bò, rau răm", 15000, StockStatus.Available),
                Prod("Bắp xào", "Bắp Mỹ xào bơ, tép khô, hành lá, ăn vặt tuổi thơ", 12000, StockStatus.Available),
                Prod("Cá viên chiên", "Cá viên chiên vàng, chấm tương ớt hoặc sốt mayonnaise (10 viên)", 15000, StockStatus.Available)));

        AddCategories(canteenList[9].Id,
            Cat("Cơm trưa",
                Prod("Cơm tấm học sinh", "Cơm tấm sườn nhỏ, trứng ốp la, đồ chua, giá phải chăng", 22000, StockStatus.Available,
                    Mg("Thêm món", false, 2, M("Thêm sườn", 10000, false), M("Thêm trứng", 5000, false))),
                Prod("Cơm gà chiên", "Gà chiên giòn, cơm trắng, rau luộc, tương ớt", 22000, StockStatus.Available),
                Prod("Cơm thịt kho trứng", "Thịt kho tàu trứng cút, cơm trắng, dưa giá", 20000, StockStatus.Available),
                Prod("Cơm chay thập cẩm", "Đậu phụ sốt, nấm, rau củ, canh rong biển", 18000, StockStatus.Available)),
            Cat("Bánh mì",
                Prod("Bánh mì thịt", "Bánh mì thịt nguội, pate, bơ, đồ chua, dưa leo, ớt", 15000, StockStatus.Available),
                Prod("Bánh mì trứng", "Bánh mì ốp la, pate, bơ, hành phi, tương ớt", 12000, StockStatus.Available),
                Prod("Bánh mì que", "Bánh mì que nhỏ, pate bơ, tương ớt, ăn nhẹ", 5000, StockStatus.Available)),
            Cat("Đồ uống",
                Prod("Trà đá", "Trà xanh mát lạnh, uống kèm cơm trưa", 2000, StockStatus.Available),
                Prod("Nước ngọt", "Nước ngọt các loại lon 330ml", 8000, StockStatus.Available),
                Prod("Sữa tươi", "Sữa tươi có đường 220ml", 10000, StockStatus.Available),
                Prod("Sữa chua uống", "Sữa chua uống vị dâu/cam/việt quất 170ml", 7000, StockStatus.Available)),
            Cat("Ăn vặt",
                Prod("Bánh bao", "Bánh bao nhân thịt trứng cút, mềm xốp, nóng hổi", 10000, StockStatus.Available),
                Prod("Bánh giò", "Bánh giò nóng, nhân thịt mộc nhĩ, ăn sáng hoặc xế", 8000, StockStatus.Available),
                Prod("Xúc xích chiên", "Xúc xích chiên vàng, chấm tương ớt (2 cây)", 10000, StockStatus.Available),
                Prod("Khoai tây chiên", "Khoai tây chiên giòn, lắc phô mai hoặc muối ớt", 12000, StockStatus.Available)));

        await db.SaveChangesAsync();

        foreach (var p in allProducts) p.SoldCount = rng.Next(0, 200);
        await db.SaveChangesAsync();

        var usedFavPairs = new HashSet<(int, int)>();
        for (int i = 0; i < 40; i++)
        {
            var userId = customers[rng.Next(customers.Count)].Id;
            var prodId = allProducts[rng.Next(allProducts.Count)].Id;
            if (usedFavPairs.Add((userId, prodId)))
            {
                db.Favorites.Add(new Favorite
                {
                    UserId = userId,
                    ProductId = prodId,
                    CreatedAt = now.AddDays(-rng.Next(1, 30))
                });
            }
        }
        await db.SaveChangesAsync();

        var orderStatuses = new[] { OrderStatus.Pending, OrderStatus.Preparing, OrderStatus.Preparing, OrderStatus.ReadyForPickup, OrderStatus.ReadyForPickup, OrderStatus.Delivered, OrderStatus.Delivered, OrderStatus.Delivered, OrderStatus.Delivered, OrderStatus.Cancelled };
        var orderTypes = new[] { OrderType.DineIn, OrderType.DineIn, OrderType.TakeAway };

        for (int i = 0; i < 120; i++)
        {
            var customer = customers[rng.Next(customers.Count)];
            var canteen = canteenList[rng.Next(canteenList.Count)];
            var canteenProducts = allProducts.Where(p => p.CanteenId == canteen.Id).ToList();
            if (canteenProducts.Count == 0) continue;

            var itemCount = rng.Next(1, 5);
            var orderItems = new List<OrderItem>();
            var total = 0m;

            var pickedProducts = new HashSet<int>();
            for (int j = 0; j < itemCount; j++)
            {
                var prod = canteenProducts[rng.Next(canteenProducts.Count)];
                if (!pickedProducts.Add(prod.Id)) continue;

                var qty = rng.Next(1, 3);
                var mgs = allModifierGroups.Where(mg => mg.ProductId == prod.Id).ToList();
                var selectedMods = new List<object>();

                foreach (var mg in mgs)
                {
                    var modsInGroup = allModifiers.Where(m => m.ModifierGroupId == mg.Id).ToList();
                    if (modsInGroup.Count == 0) continue;
                    var count = mg.Required ? Math.Max(1, rng.Next(1, Math.Min(mg.MaxSelected, modsInGroup.Count) + 1)) : rng.Next(0, Math.Min(mg.MaxSelected, modsInGroup.Count) + 1);
                    var picked = modsInGroup.OrderBy(_ => rng.Next()).Take(count).ToList();
                    var groupInfo = new { groupName = mg.Name, modifiers = picked.Select(m => new { name = m.Name, priceAmount = m.PriceAmount }).ToList() };
                    selectedMods.Add(groupInfo);
                }

                var unitPrice = prod.BasePriceAmount;
                foreach (dynamic g in selectedMods)
                    foreach (dynamic m in g.modifiers)
                        unitPrice += (decimal)m.priceAmount;
                total += unitPrice * qty;

                orderItems.Add(new OrderItem
                {
                    ProductName = prod.Name,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    SelectedModifiersJson = System.Text.Json.JsonSerializer.Serialize(selectedMods),
                    Note = rng.Next(3) == 0 ? "Ghi chú mẫu" : ""
                });
            }

            if (orderItems.Count == 0) continue;

            var status = orderStatuses[rng.Next(orderStatuses.Length)];
            var paymentStatus = status == OrderStatus.Delivered ? PaymentStatus.Paid : (rng.Next(4) == 0 ? PaymentStatus.Paid : PaymentStatus.Unpaid);
            var type = orderTypes[rng.Next(orderTypes.Length)];
            var daysAgo = rng.Next(0, 45);
            var hour = rng.Next(7, 21);
            var minute = rng.Next(0, 60);

            db.Orders.Add(new Order
            {
                UserId = customer.Id,
                CanteenId = canteen.Id,
                OrderType = type,
                Status = status,
                TotalAmount = total,
                Note = rng.Next(5) == 0 ? "Ghi chú đơn hàng" : null,
                PaymentStatus = paymentStatus,
                PickupTime = null,
                CreatedAt = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Utc).AddDays(-daysAgo),
                Items = orderItems
            });
        }
        await db.SaveChangesAsync();
    }
}
