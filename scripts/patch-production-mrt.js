const fs = require('fs');

function patchProduction() {
  const path = 'HamgamCementWeb.Server/Reports/Production.mrt';
  let s = fs.readFileSync(path, 'utf8');

  s = s.replace(
    /<Columns isList="true" count="10">\s*<value>CompanyLogo[\s\S]*?<value>CompanyPhones,System\.String<\/value>\s*<\/Columns>/,
    `<Columns isList="true" count="11">
          <value>CompanyLogo,System.String</value>
          <value>EnglishCompanyName,System.String</value>
          <value>PersianCompanyName,System.String</value>
          <value>ZmLogo,System.String</value>
          <value>PrintDate,System.String</value>
          <value>ReportTitle,System.String</value>
          <value>ReportRangeDate,System.String</value>
          <value>TotalMaterialCost,System.String</value>
          <value>TotalConversionCost,System.String</value>
          <value>GrandTotal,System.String</value>
          <value>RowCount,System.String</value>
        </Columns>`
  );

  s = s.replace(
    /<Products Ref="3"[\s\S]*?<\/Products>/,
    `<Batches Ref="3" type="Stimulsoft.Report.Dictionary.StiBusinessObject" isKey="true">
        <Alias>Batches</Alias>
        <BusinessObjects isList="true" count="0" />
        <Category />
        <Columns isList="true" count="9">
          <value>RowNumber,System.Int32</value>
          <value>ShamsiDate,System.String</value>
          <value>BatchNumber,System.String</value>
          <value>FormulaName,System.String</value>
          <value>WarehouseName,System.String</value>
          <value>MaterialCost,System.String</value>
          <value>ConversionCost,System.String</value>
          <value>TotalCost,System.String</value>
          <value>StatusLabel,System.String</value>
        </Columns>
        <Dictionary isRef="1" />
        <Guid>fd529baa3c934f8cb49e54ddf87e5484</Guid>
        <Name>Batches</Name>
      </Batches>`
  );

  s = s.replace(/<FinancialRow Ref="4"[\s\S]*?<\/FinancialRow>\s*/, '');
  s = s.replace(
    '<BusinessObjects isList="true" count="3">',
    '<BusinessObjects isList="true" count="2">'
  );

  s = s.replace('<Text>کد</Text>', '<Text>تاریخ</Text>');
  s = s.replace('<Text>نام محصول</Text>', '<Text>شماره سند</Text>');
  s = s.replace('<Text>دسته‌بندی</Text>', '<Text>فرمول</Text>');
  s = s.replace('<Text>واحد</Text>', '<Text>انبار</Text>');
  s = s.replace('<Text>موجودی</Text>', '<Text>بهای مواد</Text>');
  s = s.replace('<Text>حداقل</Text>', '<Text>بهای تبدیل</Text>');
  s = s.replace('<Text>خرید</Text>', '<Text>بهای کل</Text>');
  s = s.replace('<Text>فروش</Text>', '<Text>وضعیت</Text>');

  const binds = [
    ['{Products.RowNumber}', '{Batches.RowNumber}'],
    ['{Products.Code}', '{Batches.ShamsiDate}'],
    ['{Products.Name}', '{Batches.BatchNumber}'],
    ['{Products.Categories}', '{Batches.FormulaName}'],
    ['{Products.UnitName}', '{Batches.WarehouseName}'],
    ['{Products.StockQuantity}', '{Batches.MaterialCost}'],
    ['{Products.MinStockQuantity}', '{Batches.ConversionCost}'],
    ['{Products.PurchasePrice}', '{Batches.TotalCost}'],
    ['{Products.SalePrice}', '{Batches.StatusLabel}'],
    ['{Products.Status}', ''],
  ];
  for (const [a, b] of binds) s = s.split(a).join(b);

  const totalsBand = `        <FinancialHeaderBand Ref="26" type="HeaderBand" isKey="true">
          <Brush>Transparent</Brush>
          <ClientRectangle>0,52,200,8</ClientRectangle>
          <Components isList="true" count="3">
            <TextTot1 Ref="100" type="Text" isKey="true">
              <Brush>[230:230:230]</Brush>
              <ClientRectangle>134,1,66,6</ClientRectangle>
              <Font>B Nazanin,9</Font>
              <HorAlignment>Center</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>TextTot1</Name>
              <Page isRef="5" />
              <Parent isRef="26" />
              <Text>مواد: {Info.TotalMaterialCost}</Text>
              <TextBrush>[0:0:0]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
              <VertAlignment>Center</VertAlignment>
            </TextTot1>
            <TextTot2 Ref="101" type="Text" isKey="true">
              <Brush>[230:230:230]</Brush>
              <ClientRectangle>67,1,66,6</ClientRectangle>
              <Font>B Nazanin,9</Font>
              <HorAlignment>Center</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>TextTot2</Name>
              <Page isRef="5" />
              <Parent isRef="26" />
              <Text>تبدیل: {Info.TotalConversionCost}</Text>
              <TextBrush>[0:0:0]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
              <VertAlignment>Center</VertAlignment>
            </TextTot2>
            <TextTot3 Ref="102" type="Text" isKey="true">
              <Brush>[230:230:230]</Brush>
              <ClientRectangle>0,1,66,6</ClientRectangle>
              <Font>B Titr,9</Font>
              <HorAlignment>Center</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>TextTot3</Name>
              <Page isRef="5" />
              <Parent isRef="26" />
              <Text>جمع کل: {Info.GrandTotal}</Text>
              <TextBrush>[0:0:0]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
              <VertAlignment>Center</VertAlignment>
            </TextTot3>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>FinancialHeaderBand</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
        </FinancialHeaderBand>`;

  s = s.replace(/<FinancialHeaderBand Ref="26"[\s\S]*?<\/FinancialHeaderBand>/, totalsBand);

  const footer = `
        <FooterBand1 Ref="110" type="FooterBand" isKey="true">
          <Brush>Transparent</Brush>
          <ClientRectangle>0,95,200,8</ClientRectangle>
          <Components isList="true" count="3">
            <TextFoot1 Ref="111" type="Text" isKey="true">
              <Brush>[192:192:192]</Brush>
              <ClientRectangle>134,1,66,6</ClientRectangle>
              <Font>B Nazanin,9</Font>
              <HorAlignment>Center</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>TextFoot1</Name>
              <Page isRef="5" />
              <Parent isRef="110" />
              <Text>مواد: {Info.TotalMaterialCost}</Text>
              <TextBrush>[0:0:64]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
              <VertAlignment>Center</VertAlignment>
            </TextFoot1>
            <TextFoot2 Ref="112" type="Text" isKey="true">
              <Brush>[192:192:192]</Brush>
              <ClientRectangle>67,1,66,6</ClientRectangle>
              <Font>B Nazanin,9</Font>
              <HorAlignment>Center</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>TextFoot2</Name>
              <Page isRef="5" />
              <Parent isRef="110" />
              <Text>تبدیل: {Info.TotalConversionCost}</Text>
              <TextBrush>[0:0:64]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
              <VertAlignment>Center</VertAlignment>
            </TextFoot2>
            <TextFoot3 Ref="113" type="Text" isKey="true">
              <Brush>[192:192:192]</Brush>
              <ClientRectangle>0,1,66,6</ClientRectangle>
              <Font>B Titr,9</Font>
              <HorAlignment>Center</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>TextFoot3</Name>
              <Page isRef="5" />
              <Parent isRef="110" />
              <Text>جمع کل: {Info.GrandTotal}</Text>
              <TextBrush>[0:0:64]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
              <VertAlignment>Center</VertAlignment>
            </TextFoot3>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>FooterBand1</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
        </FooterBand1>`;

  s = s.replace(
    '        </DataBand1>\n      </Components>',
    '        </DataBand1>' + footer + '\n      </Components>'
  );
  s = s.replace(
    '<Components isList="true" count="5">',
    '<Components isList="true" count="6">'
  );

  s = s.replaceAll('ProductsReport', 'ProductionReport');
  s = s.replace('گزارش جامع محصولات', 'گزارش تولیدات');
  s = s.replace(/<ReportFile>.*?<\/ReportFile>/, '<ReportFile>Reports/Production.mrt</ReportFile>');

  s = s.replace(
    /<TextStatusHdr Ref="37"[\s\S]*?<\/TextStatusHdr>/,
    `<TextStatusHdr Ref="37" type="Text" isKey="true">
              <Brush>[192:192:192]</Brush>
              <ClientRectangle>0,0,0,6</ClientRectangle>
              <Enabled>False</Enabled>
              <Font>B Titr,8</Font>
              <Margins>0,0,0,0</Margins>
              <Name>TextStatusHdr</Name>
              <Page isRef="5" />
              <Parent isRef="27" />
              <Text></Text>
              <TextBrush>[0:0:64]</TextBrush>
              <Type>Expression</Type>
            </TextStatusHdr>`
  );
  s = s.replace(
    /<TextStatus Ref="48"[\s\S]*?<\/TextStatus>/,
    `<TextStatus Ref="48" type="Text" isKey="true">
              <Brush>Transparent</Brush>
              <ClientRectangle>0,0,0,5</ClientRectangle>
              <Enabled>False</Enabled>
              <Font>B Nazanin,8</Font>
              <Margins>0,0,0,0</Margins>
              <Name>TextStatus</Name>
              <Page isRef="5" />
              <Parent isRef="38" />
              <Text></Text>
              <TextBrush>[0:0:0]</TextBrush>
              <Type>Expression</Type>
            </TextStatus>`
  );

  s = s.replace(
    /(<Name>TextSaleHdr<\/Name>[\s\S]*?<ClientRectangle>)16,0,20,6(<\/ClientRectangle>)/,
    '$10,0,32,6$2'
  );
  // TextSaleHdr ClientRectangle is before Name - fix differently
  s = s.replace(
    /<TextSaleHdr Ref="36" type="Text" isKey="true">\s*<Brush>\[192:192:192\]<\/Brush>\s*<ClientRectangle>16,0,20,6<\/ClientRectangle>/,
    `<TextSaleHdr Ref="36" type="Text" isKey="true">
              <Brush>[192:192:192]</Brush>
              <ClientRectangle>0,0,32,6</ClientRectangle>`
  );
  s = s.replace(
    /<TextSale Ref="47" type="Text" isKey="true">\s*<Brush>Transparent<\/Brush>\s*<CanGrow>True<\/CanGrow>\s*<CanShrink>True<\/CanShrink>\s*<ClientRectangle>16,0,20,5<\/ClientRectangle>/,
    `<TextSale Ref="47" type="Text" isKey="true">
              <Brush>Transparent</Brush>
              <CanGrow>True</CanGrow>
              <CanShrink>True</CanShrink>
              <ClientRectangle>0,0,32,5</ClientRectangle>`
  );

  fs.writeFileSync(path, s);
  console.log('Production.mrt OK', {
    batches: s.includes('<Name>Batches</Name>'),
    grand: s.includes('GrandTotal'),
    footer: s.includes('FooterBand1'),
  });
}

function patchProductionBatch() {
  const path = 'HamgamCementWeb.Server/Reports/ProductionBatch.mrt';
  let s = fs.readFileSync(path, 'utf8');

  // Expand Info columns
  s = s.replace(
    /<Columns isList="true" count="7">\s*<value>CompanyLogo[\s\S]*?<value>ReportRangeDate,System\.String<\/value>\s*<\/Columns>/,
    `<Columns isList="true" count="11">
          <value>CompanyLogo,System.String</value>
          <value>EnglishCompanyName,System.String</value>
          <value>PersianCompanyName,System.String</value>
          <value>ZmLogo,System.String</value>
          <value>PrintDate,System.String</value>
          <value>ReportTitle,System.String</value>
          <value>ReportRangeDate,System.String</value>
          <value>TotalMaterialCost,System.String</value>
          <value>TotalConversionCost,System.String</value>
          <value>GrandTotal,System.String</value>
          <value>RowCount,System.String</value>
        </Columns>`
  );

  // Replace Products + FinancialRow with Batch/Input/Cost/Output
  s = s.replace(
    /<BusinessObjects isList="true" count="3">[\s\S]*?<\/BusinessObjects>/,
    `<BusinessObjects isList="true" count="5">
      <Info Ref="2" type="Stimulsoft.Report.Dictionary.StiBusinessObject" isKey="true">
        <Alias>Info</Alias>
        <BusinessObjects isList="true" count="0" />
        <Category />
        <Columns isList="true" count="11">
          <value>CompanyLogo,System.String</value>
          <value>EnglishCompanyName,System.String</value>
          <value>PersianCompanyName,System.String</value>
          <value>ZmLogo,System.String</value>
          <value>PrintDate,System.String</value>
          <value>ReportTitle,System.String</value>
          <value>ReportRangeDate,System.String</value>
          <value>TotalMaterialCost,System.String</value>
          <value>TotalConversionCost,System.String</value>
          <value>GrandTotal,System.String</value>
          <value>RowCount,System.String</value>
        </Columns>
        <Dictionary isRef="1" />
        <Guid>7163322895064661866268f391670d7b</Guid>
        <Name>Info</Name>
      </Info>
      <Batch Ref="3" type="Stimulsoft.Report.Dictionary.StiBusinessObject" isKey="true">
        <Alias>Batch</Alias>
        <BusinessObjects isList="true" count="0" />
        <Category />
        <Columns isList="true" count="11">
          <value>BatchNumber,System.String</value>
          <value>ShamsiDate,System.String</value>
          <value>FormulaName,System.String</value>
          <value>WarehouseName,System.String</value>
          <value>PlanLabel,System.String</value>
          <value>Description,System.String</value>
          <value>MaterialCost,System.String</value>
          <value>ConversionCost,System.String</value>
          <value>TotalCost,System.String</value>
          <value>StatusLabel,System.String</value>
          <value>JournalEntryNumber,System.String</value>
        </Columns>
        <Dictionary isRef="1" />
        <Guid>a1111111111111111111111111111111</Guid>
        <Name>Batch</Name>
      </Batch>
      <InputLines Ref="4" type="Stimulsoft.Report.Dictionary.StiBusinessObject" isKey="true">
        <Alias>InputLines</Alias>
        <BusinessObjects isList="true" count="0" />
        <Category />
        <Columns isList="true" count="6">
          <value>RowNumber,System.Int32</value>
          <value>ProductName,System.String</value>
          <value>WarehouseName,System.String</value>
          <value>Quantity,System.String</value>
          <value>UnitName,System.String</value>
          <value>MaterialCost,System.String</value>
        </Columns>
        <Dictionary isRef="1" />
        <Guid>b2222222222222222222222222222222</Guid>
        <Name>InputLines</Name>
      </InputLines>
      <CostLines Ref="51" type="Stimulsoft.Report.Dictionary.StiBusinessObject" isKey="true">
        <Alias>CostLines</Alias>
        <BusinessObjects isList="true" count="0" />
        <Category />
        <Columns isList="true" count="4">
          <value>RowNumber,System.Int32</value>
          <value>CostTypeLabel,System.String</value>
          <value>Description,System.String</value>
          <value>Amount,System.String</value>
        </Columns>
        <Dictionary isRef="1" />
        <Guid>c3333333333333333333333333333333</Guid>
        <Name>CostLines</Name>
      </CostLines>
      <OutputLines Ref="52" type="Stimulsoft.Report.Dictionary.StiBusinessObject" isKey="true">
        <Alias>OutputLines</Alias>
        <BusinessObjects isList="true" count="0" />
        <Category />
        <Columns isList="true" count="6">
          <value>RowNumber,System.Int32</value>
          <value>ProductName,System.String</value>
          <value>Quantity,System.String</value>
          <value>UnitName,System.String</value>
          <value>UnitCost,System.String</value>
          <value>LotCode,System.String</value>
        </Columns>
        <Dictionary isRef="1" />
        <Guid>d4444444444444444444444444444444</Guid>
        <Name>OutputLines</Name>
      </OutputLines>
    </BusinessObjects>`
  );

  // Replace FinancialHeader + ProductHeader + DataBand with multi-section layout
  const pageBody = `        <BatchHeaderBand Ref="17" type="DataBand" isKey="true">
          <Brush>Transparent</Brush>
          <BusinessObjectGuid>a1111111111111111111111111111111</BusinessObjectGuid>
          <ClientRectangle>0,52,200,22</ClientRectangle>
          <Components isList="true" count="6">
            <Bh1 Ref="60" type="Text" isKey="true">
              <Brush>Transparent</Brush>
              <ClientRectangle>100,0,100,5</ClientRectangle>
              <Font>B Nazanin,10</Font>
              <HorAlignment>Right</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>Bh1</Name>
              <Page isRef="5" />
              <Parent isRef="17" />
              <Text>شماره سند: {Batch.BatchNumber}</Text>
              <TextBrush>[0:0:0]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
            </Bh1>
            <Bh2 Ref="61" type="Text" isKey="true">
              <Brush>Transparent</Brush>
              <ClientRectangle>0,0,100,5</ClientRectangle>
              <Font>B Nazanin,10</Font>
              <HorAlignment>Right</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>Bh2</Name>
              <Page isRef="5" />
              <Parent isRef="17" />
              <Text>تاریخ: {Batch.ShamsiDate} — وضعیت: {Batch.StatusLabel}</Text>
              <TextBrush>[0:0:0]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
            </Bh2>
            <Bh3 Ref="62" type="Text" isKey="true">
              <Brush>Transparent</Brush>
              <ClientRectangle>100,5,100,5</ClientRectangle>
              <Font>B Nazanin,10</Font>
              <HorAlignment>Right</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>Bh3</Name>
              <Page isRef="5" />
              <Parent isRef="17" />
              <Text>فرمول: {Batch.FormulaName}</Text>
              <TextBrush>[0:0:0]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
            </Bh3>
            <Bh4 Ref="63" type="Text" isKey="true">
              <Brush>Transparent</Brush>
              <ClientRectangle>0,5,100,5</ClientRectangle>
              <Font>B Nazanin,10</Font>
              <HorAlignment>Right</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>Bh4</Name>
              <Page isRef="5" />
              <Parent isRef="17" />
              <Text>انبار: {Batch.WarehouseName}</Text>
              <TextBrush>[0:0:0]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
            </Bh4>
            <Bh5 Ref="64" type="Text" isKey="true">
              <Brush>Transparent</Brush>
              <ClientRectangle>0,10,200,5</ClientRectangle>
              <Font>B Nazanin,10</Font>
              <HorAlignment>Right</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>Bh5</Name>
              <Page isRef="5" />
              <Parent isRef="17" />
              <Text>برنامه: {Batch.PlanLabel} — سند دفتر: {Batch.JournalEntryNumber} — {Batch.Description}</Text>
              <TextBrush>[0:0:0]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
            </Bh5>
            <Bh6 Ref="65" type="Text" isKey="true">
              <Brush>[230:230:230]</Brush>
              <ClientRectangle>0,16,200,5</ClientRectangle>
              <Font>B Titr,9</Font>
              <HorAlignment>Center</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>Bh6</Name>
              <Page isRef="5" />
              <Parent isRef="17" />
              <Text>مواد {Info.TotalMaterialCost} | تبدیل {Info.TotalConversionCost} | جمع {Info.GrandTotal}</Text>
              <TextBrush>[0:0:64]</TextBrush>
              <TextOptions>,,RightToLeft=True,,,A=0</TextOptions>
              <Type>Expression</Type>
              <VertAlignment>Center</VertAlignment>
            </Bh6>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>BatchHeaderBand</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
          <Sort isList="true" count="0" />
        </BatchHeaderBand>
        <InputHeaderBand Ref="18" type="HeaderBand" isKey="true">
          <Brush>Transparent</Brush>
          <ClientRectangle>0,82,200,10</ClientRectangle>
          <Components isList="true" count="7">
            <IhTitle Ref="70" type="Text" isKey="true">
              <Brush>[64:64:64]</Brush>
              <ClientRectangle>0,0,200,4</ClientRectangle>
              <Font>B Titr,9</Font>
              <HorAlignment>Center</HorAlignment>
              <Margins>0,0,0,0</Margins>
              <Name>IhTitle</Name>
              <Page isRef="5" />
              <Parent isRef="18" />
              <Text>مواد مصرفی</Text>
              <TextBrush>White</TextBrush>
              <Type>Expression</Type>
              <VertAlignment>Center</VertAlignment>
            </IhTitle>
            <Ih1 Ref="71" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>188,4,12,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ih1</Name><Page isRef="5" /><Parent isRef="18" /><Text>#</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ih1>
            <Ih2 Ref="72" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>130,4,58,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ih2</Name><Page isRef="5" /><Parent isRef="18" /><Text>محصول</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ih2>
            <Ih3 Ref="73" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>90,4,40,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ih3</Name><Page isRef="5" /><Parent isRef="18" /><Text>انبار</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ih3>
            <Ih4 Ref="74" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>60,4,30,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ih4</Name><Page isRef="5" /><Parent isRef="18" /><Text>مقدار</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ih4>
            <Ih5 Ref="75" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>36,4,24,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ih5</Name><Page isRef="5" /><Parent isRef="18" /><Text>واحد</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ih5>
            <Ih6 Ref="76" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>0,4,36,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ih6</Name><Page isRef="5" /><Parent isRef="18" /><Text>بهای مواد</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ih6>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>InputHeaderBand</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
        </InputHeaderBand>
        <InputDataBand Ref="27" type="DataBand" isKey="true">
          <Brush>Transparent</Brush>
          <BusinessObjectGuid>b2222222222222222222222222222222</BusinessObjectGuid>
          <ClientRectangle>0,100,200,5</ClientRectangle>
          <Components isList="true" count="6">
            <Id1 Ref="80" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>188,0,12,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Id1</Name><Page isRef="5" /><Parent isRef="27" /><Text>{InputLines.RowNumber}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Id1>
            <Id2 Ref="81" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>130,0,58,5</ClientRectangle><Font>B Nazanin,8</Font><Margins>0,0,0,0</Margins><Name>Id2</Name><Page isRef="5" /><Parent isRef="27" /><Text>{InputLines.ProductName}</Text><TextBrush>[0:0:0]</TextBrush><TextOptions>,,RightToLeft=True,,WordWrap=True,A=0</TextOptions><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Id2>
            <Id3 Ref="82" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>90,0,40,5</ClientRectangle><Font>B Nazanin,8</Font><Margins>0,0,0,0</Margins><Name>Id3</Name><Page isRef="5" /><Parent isRef="27" /><Text>{InputLines.WarehouseName}</Text><TextBrush>[0:0:0]</TextBrush><TextOptions>,,RightToLeft=True,,WordWrap=True,A=0</TextOptions><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Id3>
            <Id4 Ref="83" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>60,0,30,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Id4</Name><Page isRef="5" /><Parent isRef="27" /><Text>{InputLines.Quantity}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Id4>
            <Id5 Ref="84" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>36,0,24,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Id5</Name><Page isRef="5" /><Parent isRef="27" /><Text>{InputLines.UnitName}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Id5>
            <Id6 Ref="85" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>0,0,36,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Id6</Name><Page isRef="5" /><Parent isRef="27" /><Text>{InputLines.MaterialCost}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Id6>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>InputDataBand</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
          <Sort isList="true" count="0" />
        </InputDataBand>
        <CostHeaderBand Ref="90" type="HeaderBand" isKey="true">
          <Brush>Transparent</Brush>
          <ClientRectangle>0,113,200,10</ClientRectangle>
          <Components isList="true" count="5">
            <ChTitle Ref="91" type="Text" isKey="true"><Brush>[64:64:64]</Brush><ClientRectangle>0,0,200,4</ClientRectangle><Font>B Titr,9</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>ChTitle</Name><Page isRef="5" /><Parent isRef="90" /><Text>هزینه‌های تبدیل</Text><TextBrush>White</TextBrush><Type>Expression</Type><VertAlignment>Center</VertAlignment></ChTitle>
            <Ch1 Ref="92" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>188,4,12,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ch1</Name><Page isRef="5" /><Parent isRef="90" /><Text>#</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ch1>
            <Ch2 Ref="93" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>120,4,68,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ch2</Name><Page isRef="5" /><Parent isRef="90" /><Text>نوع هزینه</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ch2>
            <Ch3 Ref="94" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>40,4,80,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ch3</Name><Page isRef="5" /><Parent isRef="90" /><Text>شرح</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ch3>
            <Ch4 Ref="95" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>0,4,40,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Ch4</Name><Page isRef="5" /><Parent isRef="90" /><Text>مبلغ</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Ch4>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>CostHeaderBand</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
        </CostHeaderBand>
        <CostDataBand Ref="96" type="DataBand" isKey="true">
          <Brush>Transparent</Brush>
          <BusinessObjectGuid>c3333333333333333333333333333333</BusinessObjectGuid>
          <ClientRectangle>0,131,200,5</ClientRectangle>
          <Components isList="true" count="4">
            <Cd1 Ref="97" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>188,0,12,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Cd1</Name><Page isRef="5" /><Parent isRef="96" /><Text>{CostLines.RowNumber}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Cd1>
            <Cd2 Ref="98" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>120,0,68,5</ClientRectangle><Font>B Nazanin,8</Font><Margins>0,0,0,0</Margins><Name>Cd2</Name><Page isRef="5" /><Parent isRef="96" /><Text>{CostLines.CostTypeLabel}</Text><TextBrush>[0:0:0]</TextBrush><TextOptions>,,RightToLeft=True,,WordWrap=True,A=0</TextOptions><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Cd2>
            <Cd3 Ref="99" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>40,0,80,5</ClientRectangle><Font>B Nazanin,8</Font><Margins>0,0,0,0</Margins><Name>Cd3</Name><Page isRef="5" /><Parent isRef="96" /><Text>{CostLines.Description}</Text><TextBrush>[0:0:0]</TextBrush><TextOptions>,,RightToLeft=True,,WordWrap=True,A=0</TextOptions><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Cd3>
            <Cd4 Ref="100" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>0,0,40,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Cd4</Name><Page isRef="5" /><Parent isRef="96" /><Text>{CostLines.Amount}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Cd4>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>CostDataBand</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
          <Sort isList="true" count="0" />
        </CostDataBand>
        <OutputHeaderBand Ref="101" type="HeaderBand" isKey="true">
          <Brush>Transparent</Brush>
          <ClientRectangle>0,144,200,10</ClientRectangle>
          <Components isList="true" count="7">
            <OhTitle Ref="102" type="Text" isKey="true"><Brush>[64:64:64]</Brush><ClientRectangle>0,0,200,4</ClientRectangle><Font>B Titr,9</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>OhTitle</Name><Page isRef="5" /><Parent isRef="101" /><Text>خروجی تولید</Text><TextBrush>White</TextBrush><Type>Expression</Type><VertAlignment>Center</VertAlignment></OhTitle>
            <Oh1 Ref="103" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>188,4,12,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Oh1</Name><Page isRef="5" /><Parent isRef="101" /><Text>#</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Oh1>
            <Oh2 Ref="104" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>120,4,68,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Oh2</Name><Page isRef="5" /><Parent isRef="101" /><Text>محصول</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Oh2>
            <Oh3 Ref="105" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>90,4,30,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Oh3</Name><Page isRef="5" /><Parent isRef="101" /><Text>مقدار</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Oh3>
            <Oh4 Ref="106" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>66,4,24,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Oh4</Name><Page isRef="5" /><Parent isRef="101" /><Text>واحد</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Oh4>
            <Oh5 Ref="107" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>36,4,30,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Oh5</Name><Page isRef="5" /><Parent isRef="101" /><Text>بهای واحد</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Oh5>
            <Oh6 Ref="108" type="Text" isKey="true"><Brush>[192:192:192]</Brush><ClientRectangle>0,4,36,6</ClientRectangle><Font>B Titr,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Oh6</Name><Page isRef="5" /><Parent isRef="101" /><Text>کد لات</Text><TextBrush>[0:0:64]</TextBrush><Type>Expression</Type></Oh6>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>OutputHeaderBand</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
        </OutputHeaderBand>
        <OutputDataBand Ref="109" type="DataBand" isKey="true">
          <Brush>Transparent</Brush>
          <BusinessObjectGuid>d4444444444444444444444444444444</BusinessObjectGuid>
          <ClientRectangle>0,162,200,5</ClientRectangle>
          <Components isList="true" count="6">
            <Od1 Ref="110" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>188,0,12,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Od1</Name><Page isRef="5" /><Parent isRef="109" /><Text>{OutputLines.RowNumber}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Od1>
            <Od2 Ref="111" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>120,0,68,5</ClientRectangle><Font>B Nazanin,8</Font><Margins>0,0,0,0</Margins><Name>Od2</Name><Page isRef="5" /><Parent isRef="109" /><Text>{OutputLines.ProductName}</Text><TextBrush>[0:0:0]</TextBrush><TextOptions>,,RightToLeft=True,,WordWrap=True,A=0</TextOptions><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Od2>
            <Od3 Ref="112" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>90,0,30,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Od3</Name><Page isRef="5" /><Parent isRef="109" /><Text>{OutputLines.Quantity}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Od3>
            <Od4 Ref="113" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>66,0,24,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Od4</Name><Page isRef="5" /><Parent isRef="109" /><Text>{OutputLines.UnitName}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Od4>
            <Od5 Ref="114" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>36,0,30,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Od5</Name><Page isRef="5" /><Parent isRef="109" /><Text>{OutputLines.UnitCost}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Od5>
            <Od6 Ref="115" type="Text" isKey="true"><Brush>Transparent</Brush><ClientRectangle>0,0,36,5</ClientRectangle><Font>B Nazanin,8</Font><HorAlignment>Center</HorAlignment><Margins>0,0,0,0</Margins><Name>Od6</Name><Page isRef="5" /><Parent isRef="109" /><Text>{OutputLines.LotCode}</Text><TextBrush>[0:0:0]</TextBrush><Type>DataColumn</Type><VertAlignment>Center</VertAlignment></Od6>
          </Components>
          <Conditions isList="true" count="0" />
          <Name>OutputDataBand</Name>
          <Page isRef="5" />
          <Parent isRef="5" />
          <Sort isList="true" count="0" />
        </OutputDataBand>`;

  // Replace from FinancialHeaderBand through DataBand1
  s = s.replace(
    /<FinancialHeaderBand Ref="17"[\s\S]*?<\/DataBand1>/,
    pageBody
  );

  // Page components count was 4 (header, financial, product header, data) → now 1 pageheader + 7 bands = 8
  s = s.replace(
    /(<Page1 Ref="5"[\s\S]*?<Components isList="true" count=")4(">)/,
    '$18$2'
  );

  s = s.replaceAll('JurnalReport', 'ProductionBatchReport');
  s = s.replace(/ReportAlias>.*?<\/ReportAlias>/, 'ReportAlias>ProductionBatchReport</ReportAlias>');
  s = s.replace(/ReportName>.*?<\/ReportName>/, 'ReportName>ProductionBatchReport</ReportName>');
  s = s.replace(/ReportDescription>.*?<\/ReportDescription>/, 'ReportDescription>سند تفصیلی تولید</ReportDescription>');
  s = s.replace(/<ReportFile>.*?<\/ReportFile>/, '<ReportFile>Reports/ProductionBatch.mrt</ReportFile>');

  // Batch header expressions need a DataBand for Batch OR use first row - Stimulsoft HeaderBand can bind to business object if we use DataBand for batch
  // Expressions {Batch.X} work after RegBusinessObject when Dictionary synchronized - HeaderBand expressions should resolve Info-style.
  // For list BO, use {Batch.BatchNumber} which typically takes first row in Interpretation mode.

  fs.writeFileSync(path, s);
  console.log('ProductionBatch.mrt OK', {
    input: s.includes('InputLines'),
    cost: s.includes('CostLines'),
    output: s.includes('OutputLines'),
    batch: s.includes('BatchHeaderBand'),
  });
}

patchProduction();
patchProductionBatch();
