using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Stok.Models;

namespace Stok.Services
{
    public class ExcelService
    {
        public Task<IList<Material>> ImportMaterialsAsync(Stream stream)
        {
            return Task.Run(() =>
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var headers = BuildHeaderMap(worksheet);
                var materials = new List<Material>();

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var material = new Material();
                    material.Name = ReadCell(row, headers, new[] { "name", "malzeme açıklaması", "material" }) ?? string.Empty;
                    material.Code = ReadCell(row, headers, new[] { "code", "malzeme" });
                    material.Quantity = ParseQuantity(ReadCell(row, headers, new[] { "quantity", "miktar", "qty" }));
                    material.OwnerUser = ReadCell(row, headers, new[] { "owner", "owneruser", "kullanıcı" }) ?? material.OwnerUser;
                    var description = ReadCell(row, headers, new[] { "açıklama", "description" });
                    PopulateMaterialFromDescription(material, description);

                    if (string.IsNullOrWhiteSpace(material.Name))
                    {
                        continue;
                    }

                    materials.Add(material);
                }

                return (IList<Material>)materials;
            });
        }

        public Task<Stream> ExportMaterialsAsync(IEnumerable<Material> materials)
        {
            return Task.Run<Stream>(() =>
            {
                var workbook = new XLWorkbook();
                var worksheet = workbook.AddWorksheet("Materials");
                var headers = new[] { "Name", "Code", "Serial", "Lot", "Expiry", "Quantity", "Owner" };
                for (var i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                var rowIndex = 2;
                foreach (var material in materials)
                {
                    worksheet.Cell(rowIndex, 1).Value = material.Name;
                    worksheet.Cell(rowIndex, 2).Value = material.Code ?? string.Empty;
                    worksheet.Cell(rowIndex, 3).Value = material.Serial ?? string.Empty;
                    worksheet.Cell(rowIndex, 4).Value = material.Lot ?? string.Empty;
                    worksheet.Cell(rowIndex, 5).Value = material.ExpiryDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
                    worksheet.Cell(rowIndex, 6).Value = material.Quantity;
                    worksheet.Cell(rowIndex, 7).Value = material.OwnerUser;
                    rowIndex++;
                }

                worksheet.Columns().AdjustToContents();
                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return (Stream)stream;
            });
        }

        public Task<Stream> ExportCaseRecordsAsync(IEnumerable<CaseRecord> cases)
        {
            return Task.Run<Stream>(() =>
            {
                var workbook = new XLWorkbook();
                var worksheet = workbook.AddWorksheet("Cases");
                var headers = new[] { "Hospital", "Doctor", "Patient", "Note", "CreatedAt", "CreatedBy", "UsedMaterials" };
                for (var i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                var rowIndex = 2;
                foreach (var record in cases)
                {
                    worksheet.Cell(rowIndex, 1).Value = record.Hospital;
                    worksheet.Cell(rowIndex, 2).Value = record.Doctor;
                    worksheet.Cell(rowIndex, 3).Value = record.Patient;
                    worksheet.Cell(rowIndex, 4).Value = record.Note ?? string.Empty;
                    worksheet.Cell(rowIndex, 5).Value = record.CreatedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                    worksheet.Cell(rowIndex, 6).Value = record.CreatedBy;
                    worksheet.Cell(rowIndex, 7).Value = FormatUsedMaterials(record.UsedMaterials);
                    rowIndex++;
                }

                worksheet.Columns().AdjustToContents();
                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return (Stream)stream;
            });
        }

        public Task<Stream> ExportHistoryAsync(IEnumerable<HistoryItem> history)
        {
            return Task.Run<Stream>(() =>
            {
                var workbook = new XLWorkbook();
                var worksheet = workbook.AddWorksheet("History");
                var headers = new[] { "Type", "Summary", "CreatedAt", "CreatedBy", "Details" };
                for (var i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                var rowIndex = 2;
                foreach (var item in history)
                {
                    worksheet.Cell(rowIndex, 1).Value = item.Type.ToString();
                    worksheet.Cell(rowIndex, 2).Value = item.Summary;
                    worksheet.Cell(rowIndex, 3).Value = item.CreatedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                    worksheet.Cell(rowIndex, 4).Value = item.CreatedBy;
                    worksheet.Cell(rowIndex, 5).Value = FormatUsedMaterials(item.Details);
                    rowIndex++;
                }

                worksheet.Columns().AdjustToContents();
                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return (Stream)stream;
            });
        }

        public Task<IList<ChecklistItem>> ImportChecklistAsync(Stream stream)
        {
            return Task.Run(() =>
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var headers = BuildHeaderMap(worksheet);
                var items = new List<ChecklistItem>();

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var orderText = ReadCell(row, headers, new[] { "order", "sıra", "no", "index" });
                    var patient = ReadCell(row, headers, new[] { "patient", "hasta" }) ?? string.Empty;
                    var hospital = ReadCell(row, headers, new[] { "hospital", "hastane" }) ?? string.Empty;
                    var phone = ReadCell(row, headers, new[] { "phone", "telefon" }) ?? string.Empty;
                    var timeText = ReadCell(row, headers, new[] { "time", "saat" });

                    if (string.IsNullOrWhiteSpace(patient))
                    {
                        continue;
                    }

                    var item = new ChecklistItem
                    {
                        OrderNo = ParseQuantity(orderText),
                        Patient = patient.Trim(),
                        Hospital = NormalizeHospital(hospital),
                        Phone = NormalizePhone(phone),
                        Time = ParseTime(timeText),
                        Status = ChecklistStatus.NotYet
                    };

                    items.Add(item);
                }

                return (IList<ChecklistItem>)items;
            });
        }

        public Task<Stream> ExportAllDataAsync(IEnumerable<Material> materials, IEnumerable<CaseRecord> cases, IEnumerable<HistoryItem> history, IEnumerable<ChecklistItem> checklist)
        {
            return Task.Run<Stream>(() =>
            {
                var workbook = new XLWorkbook();

                var materialsSheet = workbook.AddWorksheet("Materials");
                WriteMaterialsSheet(materialsSheet, materials);

                var casesSheet = workbook.AddWorksheet("Cases");
                WriteCasesSheet(casesSheet, cases);

                var historySheet = workbook.AddWorksheet("History");
                WriteHistorySheet(historySheet, history);

                var checklistSheet = workbook.AddWorksheet("Checklist");
                WriteChecklistSheet(checklistSheet, checklist);

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return (Stream)stream;
            });
        }

        private static IDictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in worksheet.Row(1).CellsUsed())
            {
                var header = cell.GetString().Trim();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    headerMap[header] = cell.Address.ColumnNumber;
                }
            }

            return headerMap;
        }

        private static string? ReadCell(IXLRow row, IDictionary<string, int> headerMap, IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var match = headerMap.Keys.FirstOrDefault(h => string.Equals(h, key, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    var columnIndex = headerMap[match];
                    return row.Cell(columnIndex).GetString();
                }
            }

            return null;
        }

        private static int ParseQuantity(string? value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
            {
                return quantity;
            }

            return 0;
        }

        private static void PopulateMaterialFromDescription(Material material, string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            var segments = description.Split(new[] { '\\', '/', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var text = segment.Trim();
                if (text.StartsWith("SERİ", StringComparison.OrdinalIgnoreCase))
                {
                    material.Serial = ExtractValue(text);
                }
                else if (text.StartsWith("LOT", StringComparison.OrdinalIgnoreCase))
                {
                    material.Lot = ExtractValue(text);
                }
                else if (text.StartsWith("SKT", StringComparison.OrdinalIgnoreCase) || text.StartsWith("EXP", StringComparison.OrdinalIgnoreCase))
                {
                    var value = ExtractValue(text);
                    if (DateTime.TryParseExact(value, new[] { "dd.MM.yyyy", "d.M.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        material.ExpiryDate = date;
                    }
                }
            }
        }

        private static string ExtractValue(string text)
        {
            var separatorIndex = text.IndexOf(':');
            if (separatorIndex >= 0 && separatorIndex + 1 < text.Length)
            {
                return text[(separatorIndex + 1)..].Trim();
            }

            separatorIndex = text.IndexOf('=');
            if (separatorIndex >= 0 && separatorIndex + 1 < text.Length)
            {
                return text[(separatorIndex + 1)..].Trim();
            }

            return text.Trim();
        }

        private static string FormatUsedMaterials(IEnumerable<UsedMaterialRecord> materials)
        {
            var builder = new StringBuilder();
            foreach (var material in materials)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(material.Name);
                if (!string.IsNullOrWhiteSpace(material.SerialOrLot))
                {
                    builder.Append($" ({material.SerialOrLot})");
                }

                if (material.ExpiryDate.HasValue)
                {
                    builder.Append($" SKT:{material.ExpiryDate:dd.MM.yyyy}");
                }

                builder.Append($" Qty:{material.Quantity}");
            }

            return builder.ToString();
        }

        private static void WriteMaterialsSheet(IXLWorksheet worksheet, IEnumerable<Material> materials)
        {
            var headers = new[] { "Name", "Code", "Serial", "Lot", "Expiry", "Quantity", "Owner" };
            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var rowIndex = 2;
            foreach (var material in materials)
            {
                worksheet.Cell(rowIndex, 1).Value = material.Name;
                worksheet.Cell(rowIndex, 2).Value = material.Code ?? string.Empty;
                worksheet.Cell(rowIndex, 3).Value = material.Serial ?? string.Empty;
                worksheet.Cell(rowIndex, 4).Value = material.Lot ?? string.Empty;
                worksheet.Cell(rowIndex, 5).Value = material.ExpiryDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
                worksheet.Cell(rowIndex, 6).Value = material.Quantity;
                worksheet.Cell(rowIndex, 7).Value = material.OwnerUser;
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();
        }

        private static void WriteCasesSheet(IXLWorksheet worksheet, IEnumerable<CaseRecord> cases)
        {
            var headers = new[] { "Hospital", "Doctor", "Patient", "Note", "CreatedAt", "CreatedBy", "UsedMaterials" };
            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var rowIndex = 2;
            foreach (var record in cases)
            {
                worksheet.Cell(rowIndex, 1).Value = record.Hospital;
                worksheet.Cell(rowIndex, 2).Value = record.Doctor;
                worksheet.Cell(rowIndex, 3).Value = record.Patient;
                worksheet.Cell(rowIndex, 4).Value = record.Note ?? string.Empty;
                worksheet.Cell(rowIndex, 5).Value = record.CreatedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                worksheet.Cell(rowIndex, 6).Value = record.CreatedBy;
                worksheet.Cell(rowIndex, 7).Value = FormatUsedMaterials(record.UsedMaterials);
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();
        }

        private static void WriteHistorySheet(IXLWorksheet worksheet, IEnumerable<HistoryItem> history)
        {
            var headers = new[] { "Type", "Summary", "CreatedAt", "CreatedBy", "Details" };
            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var rowIndex = 2;
            foreach (var item in history)
            {
                worksheet.Cell(rowIndex, 1).Value = item.Type.ToString();
                worksheet.Cell(rowIndex, 2).Value = item.Summary;
                worksheet.Cell(rowIndex, 3).Value = item.CreatedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                worksheet.Cell(rowIndex, 4).Value = item.CreatedBy;
                worksheet.Cell(rowIndex, 5).Value = FormatUsedMaterials(item.Details);
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();
        }

        private static void WriteChecklistSheet(IXLWorksheet worksheet, IEnumerable<ChecklistItem> items)
        {
            var headers = new[] { "Order", "Patient", "Hospital", "Phone", "Time", "Status" };
            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var rowIndex = 2;
            foreach (var item in items)
            {
                worksheet.Cell(rowIndex, 1).Value = item.OrderNo;
                worksheet.Cell(rowIndex, 2).Value = item.Patient;
                worksheet.Cell(rowIndex, 3).Value = item.Hospital;
                worksheet.Cell(rowIndex, 4).Value = item.Phone;
                worksheet.Cell(rowIndex, 5).Value = item.Time.ToString("HH\\:mm", CultureInfo.InvariantCulture);
                worksheet.Cell(rowIndex, 6).Value = item.Status.ToString();
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();
        }

        private static string NormalizeHospital(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var text = value.Trim();
            var normalized = text.ToLowerInvariant();
            if (normalized.Contains("eğitim") || normalized.Contains("egitim"))
            {
                if (normalized.Contains("araştırma") || normalized.Contains("arastirma"))
                {
                    if (normalized.Contains("hast"))
                    {
                        text = text.Replace("Eğitim ve Araştırma Hastanesi", "EAH", StringComparison.OrdinalIgnoreCase);
                        text = text.Replace("Egitim ve Arastirma Hastanesi", "EAH", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return text;
        }

        private static string NormalizePhone(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (!digits.StartsWith('0'))
            {
                digits = "0" + digits;
            }

            return digits.Length > 11 ? digits[..11] : digits;
        }

        private static TimeSpan ParseTime(string? value)
        {
            if (TimeSpan.TryParseExact(value, new[] { "hh\\:mm", "h\\:mm" }, CultureInfo.InvariantCulture, out var time))
            {
                return time;
            }

            return TimeSpan.Zero;
        }
    }
}
