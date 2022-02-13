window.onload=e=>{
	tableScroll();
}

// mimics the scroll behavior of the sidebar
function tableScroll(){
  $('#content a').each(index => {
    $('#content a')[index].addEventListener('click',e=>{
      e.preventDefault();
      console.log(e)
      console.log(e.target.attributes.href.value)
      $(e.target.attributes.href.value)[0].scrollIntoView({behavior:'smooth'})
    })
  });
}