﻿using AutoMapper;
using LibraryManagementSystem.API.Helpers;
using LMSRepository.Dto;
using LMSRepository.Helpers;
using LMSRepository.Interfaces;
using LMSService.Dto;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Policy = Role.RequireLibrarianRole)]
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public UserController(IUserRepository userRepo, IMapper mapper, IUserService userService)
        {
            _userRepo = userRepo;
            _mapper = mapper;
            _userService = userService;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<ActionResult> GetUsers()
        {
            var users = await _userService.GetUsers();

            return Ok(users);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<ActionResult> GetUser(int id)
        {
            if (!IsCurrentuser(id))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUser(id);

            return Ok(user);
        }

        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmail(email);

            return Ok(user);
        }

        [Authorize(Policy = Role.RequireLibrarianRole)]
        [HttpGet("card/{cardId}")]
        public async Task<IActionResult> GetUserByCarNumber(int cardId)
        {
            var user = await _userService.GetUserByCardNumber(cardId);

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
        {
            if (!IsCurrentuser(id))
            {
                return Unauthorized();
            }

            await _userService.UpdateMember(userForUpdateDto);

            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> AddMember(MemberForCreation memberForCreation)
        {
            var member = await _userService.AddMember(memberForCreation);

            return CreatedAtRoute("GetUser", new { id = member.Id }, member);
        }

        [HttpGet("{id}/checkouthistory")]
        public async Task<IActionResult> GetUserCheckoutHistory(int id)
        {
            if (!IsCurrentuser(id))
            {
                return Unauthorized();
            }

            var libraryCard = await _userRepo.GetUserLibraryCard(id);

            var checkouthistory = await _userRepo.GetUserCheckoutHistory(libraryCard.Id);

            var checkoutHistoryForReturn = _mapper.Map<IEnumerable<CheckoutForReturnDto>>(checkouthistory);

            return Ok(checkoutHistoryForReturn);
        }

        [HttpGet("{id}/reservehistory")]
        public async Task<IActionResult> GetUserReserveHistory(int id)
        {
            if (!IsCurrentuser(id))
            {
                return Unauthorized();
            }

            var libraryCard = await _userRepo.GetUserLibraryCard(id);

            var reserveHistory = await _userRepo.GetUserReservedAssets(libraryCard.Id);

            var reserveHistoryForReturn = _mapper.Map<IEnumerable<ReserveForReturnDto>>(reserveHistory);

            return Ok(reserveHistoryForReturn);
        }

        [HttpGet("{id}/reserves/")]
        public async Task<IActionResult> GetUserCurrentReserve(int id)
        {
            if (!IsCurrentuser(id))
            {
                return Unauthorized();
            }

            var libraryCard = await _userRepo.GetUserLibraryCard(id);

            var reserveHistory = await _userRepo.GetUserCurrentReservedAssets(libraryCard.Id);

            var reserveHistoryForReturn = _mapper.Map<IEnumerable<ReserveForReturnDto>>(reserveHistory);

            return Ok(reserveHistoryForReturn);
        }

        [HttpGet("search/")]
        public async Task<IActionResult> SearchUsers([FromQuery]string searchString)
        {
            var users = await _userService.SearchUsers(searchString);

            return Ok(users);
        }

        [HttpGet("searchMembers/")]
        public async Task<IActionResult> SearchMembers(SearchUserDto searchUser)
        {
            var users = await _userService.SearchUsers(searchUser);

            return Ok(users);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            await _userService.DeleteUser(userId);

            return NoContent();
        }

        private bool IsCurrentuser(int id)
        {
            var currentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            if (id != currentUser && !(User.IsInRole(Role.Librarian) || User.IsInRole(Role.Admin)))
            {
                return false;
            }

            return true;
        }

        [HttpGet("pagination/")]
        public async Task<IActionResult> GetAll([FromQuery]PaginationParams paginationParams)
        {
            var members = await _userService.GetAllMembersAsync(paginationParams);

            var membersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(members);

            Response.AddPagination(members.CurrentPage, members.PageSize,
                 members.TotalCount, members.TotalPages);

            return Ok(membersToReturn);
        }
    }
}