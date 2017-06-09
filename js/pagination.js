// PAGINATION INFO
pageSize = 10;
pageIndex = 1;
offset = (pageIndex - 1) * pageSize;
pageInfo =
{
    pageSize: pageSize
    , pageIndex: pageIndex
    , offset: offset
};

//STARTUP
startUp = function () {
    getByPagination(pageInfo, appendToPressTable, errorReceived);
    $(".pagination.pagination-info").on("click", "li", getPagedItems);
};

getPagedItems = function (event) {
    event.stopPropagation();
    var clickedPage = $(this).data("page");
    pageInfo = {
        pageSize: pageSize
        , pageIndex: clickedPage
        , offset: (clickedPage - 1) * pageSize
    };
    $("li.active").removeClass("active");
    $(this).addClass("active");
    getByPagination(pageInfo, appendToPressTable, errorReceived);
};

//AJAX CALLBACKS
appendToPressTable = function (responseData, sentData) {
    if (responseData && responseData.item.pagedItems) {
        pageSize = responseData.item.pageSize;
        pageIndex = sentData.pageIndex;

        //Modify the page numbers.
        var pageLinksArr = adjustPageLinks(responseData.item.totalPages, pageIndex);
        generatePageLinks(pageLinksArr, responseData.item.totalPages);

        $("#pressNodeContainer .pressNode").remove();
        for (var i = 0; i < responseData.item.pagedItems.length; i++) {
            addPressRow(responseData.item.pagedItems[i]);
        }
    }
}

errorReceived = function (data, xhr, errorThrown) {
    console.log(status + "/" + errorThrown + "/" + xhr.responseText);
}

/* Returns an array of link numbers that varies depending on the current page. */
adjustPageLinks = function (totalPages, pageIndex) {
    var numOfLinks = 5;
    var linksArr;

    if (pageIndex == 1 || pageIndex == 2)
        linksArr = [1, 2, 3, 4, 5];
    else if (pageIndex == totalPages - 1)
        linksArr = [pageIndex - 3, pageIndex - 2, pageIndex - 1, pageIndex, totalPages];
    else if (pageIndex + 1 >= totalPages)
        linksArr = [pageIndex - 4, pageIndex - 3, pageIndex - 2, pageIndex - 1, pageIndex];
    else
        linksArr = [pageIndex - 2, pageIndex - 1, pageIndex, pageIndex + 1, pageIndex + 2];

    return linksArr;
};

generatePageLinks = function (pageLinksArr, totalPages) {
    $(".pagination.pagination-info").empty();
    var prev;
    var next;

    if (pageIndex - 1 > 1)
        prev = pageIndex - 1;
    else
        prev = 1;

    if (pageIndex + 1 < totalPages)
        next = pageIndex + 1;
    else
        next = totalPages;

    var pageInfo = $(".pagination.pagination-info");

    pageInfo.append("<li data-page='1'><a href='javascript:void(0)'>first</a></li>");

    pageInfo.append("<li data-page='" + prev + "'><a href='javascript:void(0)'>prev</a></li>");

    for (var i = 0; i < pageLinksArr.length; i++) {
        if (pageIndex == pageLinksArr[i])
            pageInfo.append("<li class='active' data-page='" + pageLinksArr[i] + "'><a href='javascript:void(0)'>" + pageLinksArr[i] + "</a></li>");
        else
            pageInfo.append("<li data-page='" + pageLinksArr[i] + "'><a href='javascript:void(0)'>" + pageLinksArr[i] + "</a></li>");
    }
    pageInfo.append("<li data-page='" + next + "'><a href='javascript:void(0)'>next</a></li>");
    pageInfo.append("<li data-page='" + totalPages + "'><a href='javascript:void(0)'>last</a></li>");
};
