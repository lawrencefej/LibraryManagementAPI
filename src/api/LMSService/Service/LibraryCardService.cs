using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMSContracts.Interfaces;
using LMSEntities.DataTransferObjects;
using LMSEntities.Enumerations;
using LMSEntities.Helpers;
using LMSEntities.Models;
using LMSRepository.Data;
using LMSService.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMSService.Service
{
    public class LibraryCardService : BaseService<LibraryCardForDetailedDto>, ILibraryCardService
    {
        private readonly DataContext _context;
        private readonly ILogger<LibraryCard> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        public LibraryCardService(DataContext context, ILogger<LibraryCard> logger, UserManager<AppUser> userManager, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _mapper = mapper;
            _userManager = userManager;
            _logger = logger;
            _context = context;

        }

        public async Task<LmsResponseHandler<LibraryCardForDetailedDto>> AddLibraryCard(LibraryCardForCreationDto addCardDto)
        {

            LibraryCard card = _mapper.Map<LibraryCard>(addCardDto);

            LmsResponseHandler<AppUser> member = await CreateNewMember(card);

            if (member.Succeeded)
            {
                card = await GenerateCardNumber(card);
                card.Member = member.Item;

                _context.Add(card);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"added LibraryCard {card.CardNumber} with ID: {card.Id}");

                LibraryCardForDetailedDto cardsToReturn = _mapper.Map<LibraryCardForDetailedDto>(card);

                return LmsResponseHandler<LibraryCardForDetailedDto>.Successful(cardsToReturn);
            }

            return LmsResponseHandler<LibraryCardForDetailedDto>.Failed(member.Errors);
        }

        public async Task<LmsResponseHandler<LibraryCardForDetailedDto>> DeleteLibraryCard(int cardId)
        {
            LibraryCard card = await GetLibraryCard(cardId);

            if (card != null)
            {
                _context.Remove(card);
                await _context.SaveChangesAsync();
                // TODO log who performed the action

                _logger.LogInformation($"LibraryCard with ID {card} was deleted");
                return LmsResponseHandler<LibraryCardForDetailedDto>.Successful();
            }

            return LmsResponseHandler<LibraryCardForDetailedDto>.Failed($"Selected Card does not exist");
        }

        public async Task<PagedList<LibrarycardForListDto>> GetAllLibraryCard(PaginationParams paginationParams)
        {
            IQueryable<LibraryCard> cards = _context.LibraryCards.AsNoTracking()
                .Include(m => m.LibraryCardPhoto)
                .Include(m => m.Address)
                    .ThenInclude(s => s.State)
                .OrderBy(u => u.CardNumber).AsQueryable();

            return await FilterCards(paginationParams, cards);
        }

        public async Task<LmsResponseHandler<LibraryCardForDetailedDto>> GetLibraryCardByNumber(string cardNumber)
        {
            LibraryCard card = await _context.LibraryCards.AsNoTracking()
                .Include(m => m.Member)
                .Include(m => m.LibraryCardPhoto)
                .Include(m => m.Address)
                    .ThenInclude(s => s.State).FirstOrDefaultAsync(c => c.CardNumber == cardNumber);

            return MapLibraryCard(card);


        }

        public async Task<LmsResponseHandler<LibraryCardForDetailedDto>> GetLibraryCardById(int id)
        {
            LibraryCard card = await _context.LibraryCards.AsNoTracking()
                .Include(m => m.Member)
                .Include(m => m.LibraryCardPhoto)
                .Include(m => m.Address)
                    .ThenInclude(s => s.State).FirstOrDefaultAsync(c => c.Id == id);

            return MapLibraryCard(card);
        }

        private async Task<LibraryCard> GetLibraryCard(int id)
        {
            return await _context.LibraryCards.AsNoTracking()
                    .Include(m => m.Member)
                    .Include(m => m.LibraryCardPhoto)
                    .Include(m => m.Address)
                        .ThenInclude(s => s.State)
                    .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<PagedList<LibrarycardForListDto>> AdvancedLibraryCardSearch(LibraryCardForDetailedDto card, PaginationParams paginationParams)
        {
            IQueryable<LibraryCard> cards = _context.LibraryCards.AsNoTracking()
                .Include(m => m.LibraryCardPhoto)
                .Include(m => m.Address)
                    .ThenInclude(s => s.State)
                .OrderBy(u => u.CardNumber).AsQueryable();

            cards = cards
                .Where(x => x.FirstName.Contains(card.FirstName)
                || x.Email.Contains(card.LastName)
                || x.Email.Contains(card.Email)
                || x.PhoneNumber.Contains(card.PhoneNumber)
                );

            paginationParams.SearchString = null;

            return await FilterCards(paginationParams, cards);

        }

        public async Task<LmsResponseHandler<LibraryCardForDetailedDto>> UpdateLibraryCard(LibraryCardForUpdate cardForUpdate)
        {
            LibraryCard card = await GetLibraryCard(cardForUpdate.Id);

            if (card != null)
            {
                _mapper.Map(cardForUpdate, card);
                _context.Update(card);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"LibraryCard: {card.Id} was edited");

                return LmsResponseHandler<LibraryCardForDetailedDto>.Successful(_mapper.Map<LibraryCardForDetailedDto>(card));
            }

            return LmsResponseHandler<LibraryCardForDetailedDto>.Failed($"LibraryCard: {card.CardNumber} does not exist");
        }

        private async Task<LibraryCard> GenerateCardNumber(LibraryCard card)
        {
            card.GenerateCardNumber();

            if (await DoesLibraryCardExist(card.CardNumber))
            {
                card.GenerateCardNumber();

                await GenerateCardNumber(card);
            }

            return card;
        }

        private async Task<LmsResponseHandler<AppUser>> CreateNewMember(LibraryCard card)
        {
            AppUser member = new()
            {
                FirstName = card.FirstName,
                LastName = card.LastName,
                Created = card.Created,
                Email = card.Email,
                UserName = card.Email,
                PhoneNumber = card.PhoneNumber
            };

            IdentityResult result = await _userManager.CreateAsync(member);

            if (result.Succeeded)
            {
                result = await _userManager.AddToRoleAsync(member, nameof(UserRoles.Member));

                if (!result.Succeeded)
                {
                    return ErrorMapper<AppUser>.ReturnErrors(result.Errors);
                }

            }

            return LmsResponseHandler<AppUser>.Successful(member);
        }

        private async Task<PagedList<LibrarycardForListDto>> FilterCards(PaginationParams paginationParams, IQueryable<LibraryCard> cards)
        {

            if (!string.IsNullOrEmpty(paginationParams.SearchString))
            {
                cards = cards
                    .Where(x => x.FirstName.Contains(paginationParams.SearchString)
                    || x.LastName.Contains(paginationParams.SearchString)
                    || x.Email.Contains(paginationParams.SearchString)
                    || x.PhoneNumber.Contains(paginationParams.SearchString)
                    || x.CardNumber.Contains(paginationParams.SearchString)
                    || x.Address.Street.Contains(paginationParams.SearchString)
                    );
            }

            cards = paginationParams.SortDirection == "desc"
                ? string.Equals(paginationParams.OrderBy, "firstname", StringComparison.OrdinalIgnoreCase)
                    ? cards.OrderByDescending(x => x.FirstName)
                    : string.Equals(paginationParams.OrderBy, "lastname", StringComparison.OrdinalIgnoreCase)
                        ? cards.OrderByDescending(x => x.LastName)
                        : cards.OrderByDescending(x => x.Email)
                : string.Equals(paginationParams.OrderBy, "firstname", StringComparison.OrdinalIgnoreCase)
                    ? cards.OrderBy(x => x.FirstName)
                    : string.Equals(paginationParams.OrderBy, "lastname", StringComparison.OrdinalIgnoreCase)
                        ? cards.OrderBy(x => x.LastName)
                        : cards.OrderBy(x => x.Email);

            PagedList<LibraryCard> cardsToReturn = await PagedList<LibraryCard>.CreateAsync(cards, paginationParams.PageNumber, paginationParams.PageSize);

            return _mapper.Map<PagedList<LibrarycardForListDto>>(cardsToReturn);
        }

        private async Task<bool> DoesLibraryCardExist(string cardNumber)
        {
            return await _context.LibraryCards.AsNoTracking().AnyAsync(x => x.CardNumber == cardNumber);
        }

        private LmsResponseHandler<LibraryCardForDetailedDto> MapLibraryCard(LibraryCard card)
        {
            if (card != null)
            {
                LibraryCardForDetailedDto cardForReturn = _mapper.Map<LibraryCardForDetailedDto>(card);

                return LmsResponseHandler<LibraryCardForDetailedDto>.Successful(cardForReturn);
            }

            return LmsResponseHandler<LibraryCardForDetailedDto>.Failed("");
        }
    }
}
