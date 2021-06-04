       var ProjectionRecords = (from shoes in context.Shoes
                    where shoes.ShoeRecords.Any()
                            from records in shoes.ShoeRecords
                    where shoes.tasks.Any()
                        where shoes.tasks.FirstOrDefault().lastUpdate != null
                        let date = shoes.tasks.FirstOrDefault().lastUpdate
                        let sku = shoes.sku
                        where (records.date.Year == date.Value.Year &&
                               records.date.Month == date.Value.Month &&
                               records.date.Day == date.Value.Day && records.sku == sku)
                        select new
                       {
                           shoes = shoes,
                           records = records,
                       }
                    ).GroupBy(r=>new { r.shoes.sku,  r.records.size}); //keep it as iqueryAble 
                // if i to list here , it will waste memory and have a poor performance 
                // we will make this tolist afeter calprofit , since we try to keep it in sql raw status
                var CalProfit = (from r in ProjectionRecords //all r are groups by size
                                                             //沒貨的也會是-9999 / 0 (垃圾平台api return垃圾資料)
                                 let duCollection = r.Where(x => x.records.duprice > 0)
                                 let goatCollection = r.Where(x => x.records.goatprice > 0)
                                  //the reason i seperate it because sql throw average exception when it comes to 0     
                        //also instead of using where , we dont ignore the data , cuz i have to return to ui 
                                                             // what products are emtry 
                                 let duOri = duCollection.Any() ? duCollection.Average(c => (double)c.records.duprice) : 0
                let goatOri = goatCollection.Any() ? r.Where(x => x.records.goatprice > 0).Average(c => (double)c.records.goatprice):0
                let ratedDu = duOri * cmCur //人仔變葡
                let ratedGoat = goatOri * umCur //美金變葡
                let broughtPrice = (goatOri * 0.905 - 10) * 0.971 * umCur
                let profit = broughtPrice - ratedDu
                        select new ShoesModel()
                        {
                            state = false,
                            article_number = r.Key.sku,
                            picture = r.First().shoes.picture,
                            size = r.Key.size,
                                // here the date should be the same since they pull from api at the same time
                                // the reason i made for string is because json parse has a data type error .
                            dateTime = r.First().records.date.ToString(),
                            brand = r.First().shoes.brand,
                            duoPrice = duOri,
                            goPrice = goatOri,
                            durPrice = ratedDu,
                            grPrice = ratedGoat,
                            gbPrice = broughtPrice,
                            profit = profit,
                        }
                    ).OrderByDescending(a =>a.profit);
                //not yet , we have to do the statistics ,same reason , dont project it ToList first , keep it in sql raw
                var posQuery = CalProfit.Where(c => c.profit > 0);
                var negQuery = CalProfit.Where(c => c.profit <= 0); //0 also neg profit , cuz no money earn
                var fuckedProducts = CalProfit.Where(c => c.duoPrice == 0 || c.goPrice == 0);//either one no product sell = fucked up
                var statistics = new StatisticsModel(){
                         totalProfit = posQuery.Sum(c=>c.profit),
                         avgPos =  posQuery.Average(c=>c.profit),
                         fuckedup = fuckedProducts.Count(),
                         negativeProfit = negQuery.Sum(c=>c.profit),
                         avgNeg = negQuery.Count(),
                };
