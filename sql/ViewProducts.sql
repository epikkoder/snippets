WITH coreimages AS (
SELECT productid
     , [Main Product Image]
     , [Main Product Video]
     , [Secondary Product Image]
FROM (
	SELECT pim.productid
	     , i.imageurl
	     , mt.NAME AS mediatype
    FROM dbo.productimages pim
	INNER JOIN dbo.images i ON pim.imageid = i.id
	INNER JOIN dbo.mediatypes mt ON mt.id = i.mediatypeid) PImages
	PIVOT (Max(imageurl) FOR mediatype IN ([Main Product Image], [Main Product Video], [Secondary Product Image])) piv
)

SELECT p.Id
     , p.title
     , p.description
     , p.baseprice
     , p.createdby
     , p.createddate
     , p.modifiedby
     , p.modifieddate
     , p.producttype
     , ci.[Main Product Image]
     , ci.[Secondary Product Image]
     , ci.[Main Product Video]
FROM coreimages ci
RIGHT OUTER JOIN dbo. product p ON p.id = ci.productid
