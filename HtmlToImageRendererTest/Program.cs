using HtmlToImageRenderer;
using SkiaSharp;

Console.WriteLine("Hello, World!");

var d = DateTime.Now;

Console.WriteLine($"{d}:{d.Millisecond}");

string html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title></title>
    <style type=""text/css"">
        body {
            font-size: 6px;
            background-color: #fff;
            color: #000;
            margin: 0;
            padding: 0;
        }

        .c {
            text-align: center;
            justify-content: center;
        }

        .fr {
            justify-content: flex-end;
        }

        .bold {
            font-weight: 900;
        }

        body > div > div {
            display: flex;
        }

        .between {
            justify-content: space-between;
        }

        table {
            width: 100%;
            border-collapse: collapse;
        }

        td {
            word-break: break-all;
        }
        .bbd {
            border-bottom: 1px dotted black;
            padding-bottom:2px;
            margin-bottom:2px;
        }
    </style>
</head>

<body>
    <div style=""width: 190px; font-family: Verdana; background-color: #fff; color: #000;"">
        <div class=""c"">
            <span>***</span>
        </div>
        <br>
        <div class=""between"">
            <div>Зав.ном. ***</div><div class=""fr"">Валюта BYN</div>
        </div>
        <div class=""between"">
            <div>Рег.ном. ***</div><div class=""fr"">УНП 591025879</div>
        </div>
        <div class=""between"">
            <div>Смена №48</div><div class=""fr"">***</div>
        </div>
        <br>
        <div class=""c"">
            <span>СУТОЧНЫЙ (СМЕННЫЙ) ОТЧЕТ (Z-отчет)</span>
        </div>
        <br>
        <div class=""between"">
            <div>Смена №48 открыта:</div><div class=""fr"">2023-05-22 11:02:00</div>
        </div>
        <div class=""between bbd"">
            <div>Смена №48 закрыта:</div><div class=""fr"">2023-05-22 23:02:00</div>
        </div>
        <div class=""between"">
            <div>Итог сменных продаж:</div><div class=""fr"">1960.40</div>
        </div>
        <div class=""between"">
            <div>Платежных документов:</div><div class=""fr"">146</div>
        </div>
        <div class=""between"">
            <div>Первый платежный документ:</div><div class=""fr"">2192</div>
        </div>
        <div class=""between"">
            <div>Последний платежный документ:</div><div class=""fr"">2337</div>
        </div>
        <div class=""between bbd"">
            <div>Сумма в кассе:</div><div class=""fr"">0.00</div>
        </div>
        <div class=""between"">
            <div>Служебные внесения наличных (0 раз):</div><div class=""fr"">0.00</div>
        </div>
        <div class=""between bbd"">
            <div>Служебные изъятия наличных (1 раз):</div><div class=""fr"">1031.20</div>
        </div>
        <div class=""between"">
            <div>Отмен (0 раз):</div><div class=""fr"">0.00</div>
        </div>
        <div class=""between bbd"">
            <div>Отмен (0 раз):</div><div class=""fr"">0.00</div>
        </div>
        <table>
            <thead>
                <tr>
                    <th>Продажи</th>
                    <th>Возвраты</th>
                    <th>Аннулирования</th>
                    <th>Всего</th>
                </tr>
            </thead>
            <tbody>
                <!--@Totals-->
                <tr>
                    <td colspan=""4"" class=""bold"">Наличные</td>
                </tr>
                <tr class=""bbd"">
                    <td align=""center"">1031.20</td>
                    <td align=""center"">0.00</td>
                    <td align=""center"">0.00</td>
                    <td align=""center"">1031.20</td>
                </tr>
                <tr>
                    <td colspan=""4"" class=""bold"">Карта</td>
                </tr>
                <tr class=""bbd"">
                    <td align=""center"">837.10</td>
                    <td align=""center"">0.00</td>
                    <td align=""center"">0.00</td>
                    <td align=""center"">837.10</td>
                </tr>
                <tr>
                    <td colspan=""4"" class=""bold"">Безнал</td>
                </tr>
                <tr class=""bbd"">
                    <td align=""center"">92.10</td>
                    <td align=""center"">0.00</td>
                    <td align=""center"">0.00</td>
                    <td align=""center"">92.10</td>
                </tr>
                <tr>
                    <td colspan=""4"" class=""bold"">Итого безналичными</td>
                </tr>
                <tr class=""bbd"">
                    <td align=""center"">929.20</td>
                    <td align=""center"">0.00</td>
                    <td align=""center"">0.00</td>
                    <td align=""center"">929.20</td>
                </tr>
            </tbody>
        </table>
        <div class=""bold"">
            <span>ИТОГО</span>
        </div>
        <div class=""between"">
            <div>1960.40</div><div>0.00</div><div>0.00</div><div>1960.40</div>
        </div>
        <div class=""c"">
            <span>УИ [OutputParameters.GUID]</span>
        </div>
        <div class=""c"">
            <img src=""img_g[QR]"" width=""60px"" />
        </div>
    </div>
</body>
</html>";



string html2 = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title></title>
    <style type=""text/css"">
        body {
            font-size: 6px;
            background-color: #fff;
            color: #000;
            margin:0;
            padding:0;
        }

        .c {
            text-align: center;
            justify-content: center;
        }

        .fr {
            justify-content: flex-end;
        }

        .bold {
            font-weight: 900;
        }

        body > div > div {
            display: flex;
        }

        .between {
            justify-content: space-between;
        }
        table {
            width: 100%;
	        border-collapse: collapse;
        }
        td {
            word-break: break-all;
        }
    </style>
</head>

<body>
    <div style=""width: 190px; font-family: Verdana; background-color: #fff; color: #000;"">
        <div class=""c"">
		            <img src=""https://****"" width=""60px""/>
        </div>
        <div>
            <span>ООО '****'</span>
        </div>
        <div class=""between"">
            <div>Код УНП: ***</div><div class=""fr"">18.11.2026 0:16:05</div>
        </div>
        <div>
            <span>****</span>
        </div>
        <div class=""fr"">
            <span>Документ №***</span>
        </div>
        <div class=""c"">
            <span>======ПЛАТЕЖНЫЙ ДОКУМЕНТ======</span>
        </div>
        <div>
            <span>Чек продажи №20</span>
        </div>
        <div>
            <span>Список товаров:</span>
        </div>
        <table>
            <thead>
                <tr>
                    <th>Блюдо</th>
                    <th>Цена</th>
                    <th>Кол-во</th>
                    <th>Сумма</th>
                </tr>
            </thead>
            <tbody>
            	<tr>
                    <td colspan=""4"">Очень длинное название продукта в несколько строк</td>
                </tr>
                <tr>
                    <td>Чесночный</td>
                    <td>3,45</td>
                    <td>1,00</td>
                    <td align=""center"">3,11</td>
                </tr>
		<tr class="""">
			<td></td>
			<td></td>
                    	<td>Скидка:</td>
		    	<td align=""center"">0,34</td>
		</tr>
		<tr style=""border-bottom: dashed black 1px;"">
		</tr><tr>
                    <td colspan=""4"">Тест #08 Длинноватое название</td>
                </tr>
                <tr>
                    <td></td>
                    <td>8,00</td>
                    <td>1,00</td>
                    <td align=""center"">7,20</td>
                </tr>
		<tr class="""">
			<td></td>
			<td></td>
                    	<td>Скидка:</td>
		    	<td align=""center"">0,80</td>
		</tr>
		<tr style=""border-bottom: dashed black 1px;"">
		</tr><tr>
                    <td colspan=""4"">Очень длинное название продукта в несколько строк</td>
                </tr>
                <tr>
                    <td>Фирменный, Чили, Без лука, Без всего, </td>
                    <td>29,30</td>
                    <td>1,00</td>
                    <td align=""center"">26,37</td>
                </tr>
		<tr class="""">
			<td></td>
			<td></td>
                    	<td>Скидка:</td>
		    	<td align=""center"">2,93</td>
		</tr>
		<tr style=""border-bottom: dashed black 1px;"">
		</tr>
            </tbody>
        </table>
        <div class=""between bold"">
            <span>Итого по чеку:</span><span>40,75</span>
        </div>
        <div class=""between bold"">
            <span>Итого скидки:</span><span>4,07</span>
        </div>
        <div class=""between bold"">
            <span>Итого к оплате:</span><span>36,68</span>
        </div>
        <div>
            <span>Оплачено:</span>
        </div>
        <table>
            <tbody>
            	<tr class=""bold"">
                    <td>Наличные</td>
                    <td align=""right"">36,68</td>
                </tr>
		<tr class="""">
                    <td style=""padding-left:5px;""> Внесено</td>
		    <td align=""right"">40,00</td>
		</tr>
		<tr class="""">
                    <td style=""padding-left:5px;""> Сдача</td>
		    <td align=""right"">3,32</td>
		</tr>
            </tbody>
        </table>
        <div>
            <span>Кассир: Супер повар на тринити быстрый как пуля</span>
        </div>
        <div>
            <span>18.11.2026 0:16:05</span>
        </div>
        <div class=""c"">
            <span>Зав.ном. ***</span>
        </div>
        <div class=""c"">
            <span>Рег.ном. ***</span>
        </div>
        <div class=""c"">
            <span>УИ ***</span>
        </div>
        <div class=""c"">
            <img src=""data:image/jpeg; base64,***"" width=""60px""/>
        </div>
    </div>
</body>
</html>";








string html3 = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title></title>
    <style type=""text/css"">
        body {
            font-size: 6px;
            background-color: #fff;
            color: #000;
            margin:0;
            padding:0;
        }

        .bold {
            font-weight: 900;
        }

        body > div > div {
            display: flex;
        }

        .between {
            justify-content: space-between;
        }
    </style>
</head>

<body>
    <div style=""width: 190px; font-family: Verdana; background-color: #fff; color: #000;"">
        <div class=""between bold"">
            <span>Итого по чеку:</span><span>40,75</span>
        </div>
        <div class=""between bold"">
            <span>Итого скидки:</span><span>4,07</span>
        </div>
        <div class=""between bold"">
            <span>Итого к оплате:</span><span>36,68</span>
        </div>
    </div>
</body>
</html>";





var renderer = new HtmlRenderer(new HtmlRendererOptions
{
    Width = 800,
    DeviceScaleFactor = 3f,          // ретина
    BackgroundColor = SKColors.White,
    BaseUri = "localhost", TrimBackground = true, Monochrome = true
});

byte[] png2 = await renderer.RenderToPngAsync(html);  // асинхронно
await File.WriteAllBytesAsync("file.png", png2);


d = DateTime.Now;

Console.WriteLine($"{d}:{d.Millisecond}");

Console.WriteLine($"*****************************");

d = DateTime.Now;

Console.WriteLine($"{d}:{d.Millisecond}");

png2 = await renderer.RenderToPngAsync(html2);  // асинхронно
await File.WriteAllBytesAsync("file.png", png2);

d = DateTime.Now;

Console.WriteLine($"{d}:{d.Millisecond}");


Console.WriteLine($"*****************************");

d = DateTime.Now;

Console.WriteLine($"{d}:{d.Millisecond}");

png2 = await renderer.RenderToPngAsync(html2);  // асинхронно
await File.WriteAllBytesAsync("file2.png", png2);

d = DateTime.Now;

Console.WriteLine($"{d}:{d.Millisecond}");









Console.WriteLine($"*****************************");

d = DateTime.Now;

Console.WriteLine($"{d}:{d.Millisecond}");

png2 = await renderer.RenderToPngAsync(html3);  // асинхронно
await File.WriteAllBytesAsync("file3.png", png2);

d = DateTime.Now;

Console.WriteLine($"{d}:{d.Millisecond}");




Console.ReadKey();